/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using FluentResults;
using Grpc.Core;

namespace Dgraph.Transactions
{
    internal class Transaction : ITransaction
    {
        TransactionState IQuery.TransactionState => TransactionState;
        private TransactionState TransactionState;
        private readonly IDgraphClientInternal Client;
        private readonly Api.TxnContext Context;
        private readonly bool ReadOnly;
        private readonly bool BestEffort;

        private bool HasMutated;

        internal Transaction(IDgraphClientInternal client, bool readOnly = false, bool bestEffort = false)
        {
            Client = client;
            ReadOnly = readOnly;
            BestEffort = bestEffort;
            TransactionState = TransactionState.OK;
            Context = new Api.TxnContext();
        }

        #region IQuery

        Task<Result<Response>> IQuery.QueryWithVars(
            string queryString,
            Dictionary<string, string> varMap,
            CallOptions? options
        )
        {
            var request = new Api.Request
            {
                Query = queryString,
                RespFormat = Api.Request.Types.RespFormat.Json
            };
            if (varMap is not null)
            {
                request.Vars.Add(varMap);
            }
            return (this as ITransaction).Do(request, options);
        }

        Task<Result<Response>> IQuery.QueryRDFWithVars(
            string queryString,
            Dictionary<string, string> varMap,
            Grpc.Core.CallOptions? options
        )
        {
            var request = new Api.Request
            {
                Query = queryString,
                RespFormat = Api.Request.Types.RespFormat.Rdf
            };
            if (varMap is not null)
            {
                request.Vars.Add(varMap);
            }
            return (this as ITransaction).Do(request, options);
        }

        #endregion

        #region ITransaction

        async Task<Result<Response>> ITransaction.Do(Api.Request request, CallOptions? options)
        {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK)
            {
                return Result.Fail<Response>(new TransactionNotOK(TransactionState.ToString()));
            }

            if (string.IsNullOrWhiteSpace(request.Query) && request.Mutations.Count == 0)
            {
                return Result.Ok(new Response(new Api.Response()));
            }

            if (request.Mutations.Count > 0)
            {
                if (ReadOnly)
                {
                    return Result.Fail<Response>(new TransactionReadOnly());
                }

                // If all of the request's mutations are set to CommitNow, then set the request to CommitNow.
                if (request.Mutations.All(r => r.CommitNow))
                {
                    request.CommitNow = true;
                }
                // Otherwise, if some of the mutations are set to CommitNow and some are not, 
                // then this is an ambiguous state for the request, so return a failed response.
                else if (request.Mutations.Any(r => r.CommitNow))
                {
                    return Result.Fail<Response>(
                        new TransactionMalformed("CommitNow on request mutations must be all true or all false.")
                    );
                }

                HasMutated = true;
            }

            request.StartTs = Context.StartTs;
            request.Hash = Context.Hash;
            request.BestEffort = BestEffort;
            request.ReadOnly = ReadOnly;

            var response = await Client.DgraphExecute(
                async (dg) => Result.Ok<Response>(
                    new Response(await dg.QueryAsync(
                        request,
                        options ?? new CallOptions())
                )),
                (rpcEx) => Result.Fail<Response>(new ExceptionalError(rpcEx))
            );

            if (response.IsFailed)
            {
                if (!ReadOnly && request.Mutations.Count > 0)
                {
                    await (this as ITransaction).Discard(); // Ignore error - user should see the original error.
                    TransactionState = TransactionState.Error; // overwrite the aborted value
                }

                return response;
            }

            if (request.CommitNow)
            {
                TransactionState = TransactionState.Committed;
            }

            var err = MergeContext(response.Value.DgraphResponse.Txn);

            if (err.IsSuccess)
            {
                return response;
            }
            else
            {
                return err.ToResult<Response>();
            }
        }

        // Dispose method - Must be ok to call multiple times!
        async Task<Result> ITransaction.Discard(CallOptions? options)
        {
            if (TransactionState != TransactionState.OK)
            {
                // TransactionState.Committed can't be discarded
                // TransactionState.Error only entered after Discard() is already called.
                // TransactionState.Aborted multiple Discards have no effect
                return Result.Ok();
            }

            TransactionState = TransactionState.Aborted;

            if (!HasMutated)
            {
                return Result.Ok();
            }

            Context.Aborted = true;

            return await Client.DgraphExecute(
                async (dg) =>
                {
                    await dg.CommitOrAbortAsync(
                        Context,
                        options ?? new CallOptions());
                    return Result.Ok();
                },
                (rpcEx) => Result.Fail(new ExceptionalError(rpcEx))
            );
        }

        async Task<Result> ITransaction.Commit(CallOptions? options)
        {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK)
            {
                return Result.Fail(new TransactionNotOK(TransactionState.ToString()));
            }

            TransactionState = TransactionState.Committed;

            if (!HasMutated)
            {
                return Result.Ok();
            }

            return await Client.DgraphExecute(
                async (dg) =>
                {
                    await dg.CommitOrAbortAsync(
                        Context,
                        options ?? new CallOptions());
                    return Result.Ok();
                },
                (rpcEx) => Result.Fail(new ExceptionalError(rpcEx))
            );
        }

        #endregion

        #region IDisposable

        private bool Disposed;

        private void AssertNotDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        void IDisposable.Dispose()
        {
            if (!Disposed && TransactionState == TransactionState.OK)
            {
                // This makes Discard run async (maybe another thread)  So the current thread 
                // might exit and get back to work (we don't really care how the Discard() went).
                // But, this could race with disposal of everything, if this disposal is running
                // with whole program shutdown.  I don't think this matters because Dgraph will
                // clean up the transaction at some point anyway and if we've exited the program, 
                // we don't care.
                Task.Run(() => (this as ITransaction).Discard());
            }

            Disposed = true;
        }

        #endregion

        private Result MergeContext(Api.TxnContext srcContext)
        {
            if (srcContext == null)
            {
                return Result.Ok();
            }

            if (Context.StartTs == 0)
            {
                Context.StartTs = srcContext.StartTs;
            }

            if (Context.StartTs != srcContext.StartTs)
            {
                return Result.Fail(new StartTsMismatch());
            }

            Context.Hash = srcContext.Hash;

            Context.Keys.Add(srcContext.Keys);
            Context.Preds.Add(srcContext.Preds);

            return Result.Ok();
        }
    }
}

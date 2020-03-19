/*
 * Copyright 2020 Dgraph Labs, Inc. and Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
using System;
using System.Threading;
using System.Threading.Tasks;
using Api;
using FluentResults;
using Grpc.Core;

namespace DgraphDotNet.Transactions
{

    internal class Transaction : ReadOnlyTransaction, ITransaction {

        private bool HasMutated;

        internal Transaction(IDgraphClientInternal client) : base(client, false, false) { }

        public async Task<FluentResults.Result<Api.Response>> Mutate(
            RequestBuilder request, 
            CallOptions? options = null
        ) {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail<Api.Response>(new TransactionNotOK(TransactionState.ToString()));
            }

            var req = request.Request;
            if (req.Mutations.Count == 0) {
                return Results.Ok<Api.Response>(new Response());
            }

            HasMutated = true;

            req.StartTs = Context.StartTs;

            var response = await Client.DgraphExecute(
                async (dg) => Results.Ok<Api.Response>(await dg.QueryAsync(req)),
                (rpcEx) => Results.Fail<Api.Response>(new FluentResults.ExceptionalError(rpcEx))
            );

            if(response.IsFailed) {
                await Discard(); // Ignore error - user should see the original error.

                TransactionState = TransactionState.Error; // overwrite the aborted value
                return response;
            }
                
            if (req.CommitNow) {
                TransactionState = TransactionState.Committed;
            }

            var err = MergeContext(response.Value.Txn);
            if (err.IsFailed) {
                // The WithReasons() here will turn this Ok, into a Fail.  So the result 
                // and an error are in there like the Go lib.  But this can really only
                // occur on an internal Dgraph error, so it's really an error
                // and there's no need to code for cases to dig out the value and the 
                // error - just 
                //   if (...IsFailed) { ...assume mutation failed...}
                // is enough.
                return Results.Ok<Api.Response>(response.Value).WithReasons(err.Reasons);
            }

            return Results.Ok<Api.Response>(response.Value);
        }

        // Dispose method - Must be ok to call multiple times!
        public async Task<FluentResults.Result> Discard(CallOptions? options = null) {
            if (TransactionState != TransactionState.OK) {
                // TransactionState.Committed can't be discarded
                // TransactionState.Error only entered after Discard() is already called.
                // TransactionState.Aborted multiple Discards have no effect
                return Results.Ok();
            }

            TransactionState = TransactionState.Aborted;

            if (!HasMutated) {
                return Results.Ok();
            }

            Context.Aborted = true;

            return await Client.DgraphExecute(
                async (dg) => { 
                    await dg.CommitOrAbortAsync(
                        Context,
                        options ?? new CallOptions(null, null, default(CancellationToken)));
                    return Results.Ok();
                },
                (rpcEx) => Results.Fail(new FluentResults.ExceptionalError(rpcEx))
            );
        }

        public async Task<FluentResults.Result> Commit(CallOptions? options = null) {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail(new TransactionNotOK(TransactionState.ToString()));
            }

            TransactionState = TransactionState.Committed;

            if (!HasMutated) {
                return Results.Ok();
            }

            return await Client.DgraphExecute(
                async (dg) => { 
                    await dg.CommitOrAbortAsync(
                        Context,
                        options ?? new CallOptions(null, null, default(CancellationToken)));
                    return Results.Ok();
                },
                (rpcEx) => Results.Fail(new FluentResults.ExceptionalError(rpcEx))
            );
        }

        // 
        // ------------------------------------------------------
        //              disposable pattern.
        // ------------------------------------------------------
        //
        #region disposable pattern

        private bool Disposed;

        protected override void AssertNotDisposed() {
            if (Disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {

            if (!Disposed && TransactionState == TransactionState.OK) {
                Disposed = true;

                // This makes Discard run async (maybe another thread)  So the current thread 
                // might exit and get back to work (we don't really care how the Discard() went).
                // But, this could race with disposal of everything, if this disposal is running
                // with whole program shutdown.  I don't think this matters because Dgraph will
                // clean up the transaction at some point anyway and if we've exited the program, 
                // we don't care.
                Task.Run(() => Discard());
            }
        }

        #endregion

    }
}
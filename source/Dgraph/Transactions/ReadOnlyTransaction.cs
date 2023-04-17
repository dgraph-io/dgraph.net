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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api;
using FluentResults;
using Grpc.Core;

namespace Dgraph.Transactions
{

    internal class ReadOnlyTransaction : IQuery
    {

        public TransactionState TransactionState { get; protected set; }

        protected readonly IDgraphClientInternal Client;

        protected readonly TxnContext Context;

        protected readonly Boolean ReadOnly;

        protected readonly Boolean BestEffort;

        internal ReadOnlyTransaction(IDgraphClientInternal client, bool bestEffort) : this(client, true, bestEffort) { }

        protected ReadOnlyTransaction(IDgraphClientInternal client, Boolean readOnly, Boolean bestEffort)
        {
            Client = client;
            ReadOnly = readOnly;
            BestEffort = bestEffort;
            TransactionState = TransactionState.OK;
            Context = new TxnContext();
        }

        public async Task<FluentResults.Result<Response>> Query(
            string queryString,
            CallOptions? options = null
        )
        {
            return await QueryWithVars(queryString, new Dictionary<string, string>(), options);
        }

        public async Task<FluentResults.Result<Response>> QueryWithVars(
            string queryString,
            Dictionary<string, string> varMap,
            CallOptions? options = null
        )
        {

            AssertNotDisposed();

            if (TransactionState != TransactionState.OK)
            {
                return Results.Fail<Response>(
                    new TransactionNotOK(TransactionState.ToString()));
            }

            try
            {
                Api.Request request = new Api.Request();
                request.Query = queryString;
                request.Vars.Add(varMap);
                request.StartTs = Context.StartTs;
                request.Hash = Context.Hash;
                request.ReadOnly = ReadOnly;
                request.BestEffort = BestEffort;

                var response = await Client.DgraphExecute(
                    async (dg) =>
                        Results.Ok<Response>(
                            new Response(await dg.QueryAsync(
                                request,
                                options ?? new CallOptions(null, null, default(CancellationToken)))
                        )),
                    (rpcEx) =>
                        Results.Fail<Response>(new FluentResults.ExceptionalError(rpcEx))
                );

                if (response.IsFailed)
                {
                    return response;
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
            catch (Exception ex)
            {
                return Results.Fail<Response>(new FluentResults.ExceptionalError(ex));
            }
        }

        protected FluentResults.Result MergeContext(TxnContext srcContext)
        {
            if (srcContext == null)
            {
                return Results.Ok();
            }

            if (Context.StartTs == 0)
            {
                Context.StartTs = srcContext.StartTs;
            }

            if (Context.StartTs != srcContext.StartTs)
            {
                return Results.Fail(new StartTsMismatch());
            }

            Context.Hash = srcContext.Hash;

            Context.Keys.Add(srcContext.Keys);
            Context.Preds.Add(srcContext.Preds);

            return Results.Ok();
        }

        protected virtual void AssertNotDisposed() { }

    }

}
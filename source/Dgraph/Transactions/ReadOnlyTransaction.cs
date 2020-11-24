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
using System.Threading.Tasks;
using FluentResults;
using Grpc.Core;

namespace Dgraph.Transactions
{

    internal class ReadOnlyTransaction : IQuery {

        public TransactionState TransactionState { get; protected set; }

        protected readonly IDgraphClientInternal _client;

        protected readonly Api.TxnContext _context;

        private readonly Boolean _readOnly;

        private readonly Boolean _bestEffort;

        internal ReadOnlyTransaction(IDgraphClientInternal client, bool bestEffort) : this(client, true, bestEffort) { }

        protected ReadOnlyTransaction(IDgraphClientInternal client, Boolean readOnly, Boolean bestEffort) {
            _client = client;
            _readOnly = readOnly;
            _bestEffort = bestEffort;
            TransactionState = TransactionState.OK;
            _context = new Api.TxnContext();
        }

        public async Task<Result<Response>> Query(
            string queryString, 
            CallOptions? options = null
        ) {
            return await QueryWithVars(queryString, new Dictionary<string, string>(), options);
        }

        public async Task<Result<Response>> QueryWithVars(
            string queryString, 
            Dictionary<string, string> varMap,
            CallOptions? options = null
        ) {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail<Response>(
                    new TransactionNotOK(TransactionState.ToString()));
            }

            try {
                Api.Request request = new Api.Request();
                request.Query = queryString;
                request.Vars.Add(varMap);
                request.StartTs = _context.StartTs;
                request.ReadOnly = _readOnly;
                request.BestEffort = _bestEffort;

                var response = await _client.QueryAsync(request, options);

                if(response.IsFailed) {
                    return response;
                }

                var err = MergeContext(response.Value.DgraphResponse.Txn);

                if (err.IsSuccess) {
                    return response;
                } else {
                    return err.ToResult<Response>();
                }

            } catch (Exception ex) {
                return Results.Fail<Response>(new ExceptionalError(ex));
            }
        }

        protected Result MergeContext(Api.TxnContext srcContext) {
            if (srcContext == null) {
                return Results.Ok();
            }

            if (_context.StartTs == 0) {
                _context.StartTs = srcContext.StartTs;
            }

            if (_context.StartTs != srcContext.StartTs) {
                return Results.Fail(new StartTsMismatch());
            }

            _context.Keys.Add(srcContext.Keys);
            _context.Preds.Add(srcContext.Preds);

            return Results.Ok();
        }

        protected virtual void AssertNotDisposed() { }

    }

}
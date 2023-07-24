/*
 * Copyright 2023 Dgraph Labs, Inc. and Contributors
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
using Dgraph.Transactions;
using FluentResults;
using Grpc.Core;
using NSubstitute;

namespace Dgraph.tests.Transactions
{
    public class TransactionFixtureBase
    {
        internal (IDgraphClientInternal, Response) MinimalClient()
        {
            var client = Substitute.For<IDgraphClientInternal>();

            var dgResp = new Api.Response();
            dgResp.Txn = new Api.TxnContext();
            var response = new Response(dgResp);
            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(Result.Ok(response));

            return (client, response);
        }

        protected List<Func<Task<ResultBase>>> GetAllTestFunctions(ITransaction txn) =>
            new List<Func<Task<ResultBase>>> {
                async () => await txn.Do(new RequestBuilder().
                    WithMutations(new MutationBuilder().SetJson("json"))),
                async () => await txn.Query("query"),
                async () => await txn.QueryWithVars("query", null),
                async () => await txn.Commit()
            };
    }
}

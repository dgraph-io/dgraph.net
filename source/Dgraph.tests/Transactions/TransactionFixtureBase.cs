/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
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

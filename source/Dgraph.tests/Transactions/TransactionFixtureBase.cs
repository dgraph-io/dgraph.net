using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api;
using Dgraph.Transactions;
using FluentResults;
using Grpc.Core;
using NSubstitute;

namespace Dgraph.tests.Transactions
{
    public class TransactionFixtureBase {

        internal (IDgraphClientInternal, Response) MinimalClient() {
            var client = Substitute.For<IDgraphClientInternal>();

            var response = new Response();
            response.Txn = new TxnContext();
            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(Results.Ok(response));

            return (client, response);
        }

        protected List<Func<Task<ResultBase>>> GetAllTestFunctions(ITransaction txn) =>
            new List<Func<Task<ResultBase>>> {
                async () => await txn.Mutate(new RequestBuilder().
                    WithMutations(new MutationBuilder{ SetJson = "json" })),
                async () => await txn.Query("query"),
                async () => await txn.QueryWithVars("query", null),
                async () => await txn.Commit()
            };
    }
}
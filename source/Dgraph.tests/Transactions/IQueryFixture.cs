using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dgraph.Transactions;
using FluentAssertions;
using FluentResults;
using Google.Protobuf;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph.tests.Transactions
{
    public class IQueryFixture : TransactionFixtureBase {

        // [Test]
        // public async Task Query_PassesOnQuery() {
        //     // again can't really do this without a proxy interface for Dgraph
        // }

        // [Test]
        // public async Task Query_PassesOnQueryAndVariables() {

        // }

        [Test]
        public async Task Query_PassesBackResult() {
            (var client, var response) = MinimalClient();
            var txn = new Transaction(client);

            response.DgraphResponse.Json = ByteString.CopyFromUtf8("json"); 
            // ??ByteString.CopyFrom(Encoding.UTF8.GetBytes(json));

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Results.Ok(response));

            var result = await txn.QueryWithVars(
                "query", 
                new Dictionary<string, string> { { "var", "val" } });

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(response);
        }

        [Test]
        public async Task Query_FailsIfError() {
            (var client, _) = MinimalClient();
            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Results.Fail(new ExceptionalError(new RpcException(new Status(), "Something failed"))));

            var result = await (new Transaction(client)).Query("throw");

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public async Task Query_FailDoesntChangeTransactionOKState() {
            (var client, _) = MinimalClient();
            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Results.Fail(new ExceptionalError(new RpcException(new Status(), "Something failed"))));

            var txn = new Transaction(client);
            var result = await txn.Query("throw");

            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        [Test]
        public async Task Query_SuccessDoesntChangeTransactionOKState() {
            (var client, var response) = MinimalClient();
            var txn = new Transaction(client);

            await txn.QueryWithVars(
                "query", 
                new Dictionary<string, string> { { "var", "val" } });

            txn.TransactionState.Should().Be(TransactionState.OK);
        }

    }
}
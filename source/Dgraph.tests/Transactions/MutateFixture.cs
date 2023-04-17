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

    public class MutateFixture : TransactionFixtureBase
    {

        [Test]
        public async Task Mutate_EmptyMutationDoesNothing()
        {
            (var client, _) = MinimalClient();
            var txn = new Transaction(client);

            var req = new RequestBuilder();
            await txn.Mutate(req);

            await client.DidNotReceive().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>());
            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        [Test]
        public async Task Mutate_CommitNowChangesStateToCommitted()
        {
            (var client, _) = MinimalClient();
            var txn = new Transaction(client);

            var req = new RequestBuilder()
            {
                CommitNow = true
            }.WithMutations(new MutationBuilder { SetJson = "json" });
            await txn.Mutate(req);

            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

        // [Test]
        // public async Task Mutate_PassesOnMutation() {
        //     // again can't really do this without a proxy interface for Dgraph
        // }


        [Test]
        public async Task Mutate_FailsOnException()
        {
            (var client, _) = MinimalClient();

            var txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder { SetJson = "json" });
            await txn.Mutate(req);

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Results.Fail(new ExceptionalError(new RpcException(new Status(), "Something failed"))));

            var result = await txn.Mutate(req);

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public async Task Mutate_PassesBackResult()
        {
            (var client, var assigned) = MinimalClient();
            var txn = new Transaction(client);

            var response = new Api.Response() { Json = ByteString.CopyFromUtf8("json") };
            response.Uids.Add(new Dictionary<string, string> { { "node1", "0x1" } });

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Results.Ok(new Response(response)));

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder { SetJson = "json" });
            await txn.Mutate(req);
            var result = await txn.Mutate(req);

            result.IsSuccess.Should().BeTrue();
            result.Value.DgraphResponse.Should().BeEquivalentTo(response);
        }

    }
}
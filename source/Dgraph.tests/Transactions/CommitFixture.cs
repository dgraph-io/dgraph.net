using System;
using System.Linq;
using System.Threading.Tasks;
using Dgraph.Transactions;
using FluentAssertions;
using FluentResults;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph.tests.Transactions
{
    public class CommitFixture : TransactionFixtureBase
    {

        [Test]
        public async Task Commit_SetsTransactionStateToCommitted()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Commit();

            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

        [Test]
        public async Task Commit_ClientNotReceiveCommitIfNoMutation()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Commit();

            await client.DidNotReceive().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>());
        }

        [Test]
        public async Task Commit_ClientReceivedCommitIfMutation()
        {
            (var client, _) = MinimalClient();

            var txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder { SetJson = "json" });
            await txn.Mutate(req);
            await txn.Commit();

            // FIXME: can't really test what the Dgraph got without
            // adding an interface in front of the actual Dgraph client
            await client.Received().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>());
        }

        [Test]
        public async Task Commit_FailsOnException()
        {
            (var client, _) = MinimalClient();

            var txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder { SetJson = "json" });
            await txn.Mutate(req);

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>()).Returns(
                    Results.Fail(new ExceptionalError(
                        new RpcException(new Status(), "Something failed"))));
            var result = await txn.Commit();

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

    }
}
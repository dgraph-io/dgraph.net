using System;
using System.Threading.Tasks;
using Api;
using Dgraph.Transactions;
using FluentAssertions;
using FluentResults;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph.tests.Transactions
{
    public class TransactionFixture : TransactionFixtureBase {

        [Test]
        public async Task All_FailIfAlreadyCommitted() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Commit();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public async Task All_FailIfDiscarded() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Discard();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public void All_ExceptionIfDisposed() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            txn.Dispose();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                test.Should().Throw<ObjectDisposedException>();
            }
        }

        [Test]
        public async Task All_FailIfTransactionError() {
            // force transaction into error state
            var client = Substitute.For<IDgraphClientInternal>();
            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Results.Fail(new ExceptionalError(
                        new RpcException(new Status(), "Something failed"))));
            var txn = new Transaction(client);
            
            var req = new RequestBuilder().
                WithMutations(new MutationBuilder{ SetJson = "json" });
            await txn.Mutate(req);

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

    }
}
/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Threading.Tasks;
using Dgraph.Transactions;
using FluentAssertions;
using FluentResults;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph.tests.Transactions
{
    public class TransactionFixture : TransactionFixtureBase
    {

        [Test]
        public async Task All_FailIfAlreadyCommitted()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            ITransaction txn = new Transaction(client);
            await txn.Commit();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests)
            {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public async Task All_FailIfDiscarded()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            ITransaction txn = new Transaction(client);
            await txn.Discard();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests)
            {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public async Task All_ExceptionIfDisposed()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            ITransaction txn = new Transaction(client);
            txn.Dispose();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests)
            {
                await test.Should().ThrowAsync<ObjectDisposedException>();
            }
        }

        [Test]
        public async Task All_FailIfTransactionError()
        {
            // force transaction into error state
            var client = Substitute.For<IDgraphClientInternal>();
            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Result.Fail(new ExceptionalError(
                        new RpcException(new Status(), "Something failed"))));
            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests)
            {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

    }
}

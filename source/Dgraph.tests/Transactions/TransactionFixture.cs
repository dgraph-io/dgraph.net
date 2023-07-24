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

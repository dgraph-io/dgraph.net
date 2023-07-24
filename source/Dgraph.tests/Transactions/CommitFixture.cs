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
            ITransaction txn = new Transaction(client);
            await txn.Commit();

            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

        [Test]
        public async Task Commit_ClientNotReceiveCommitIfNoMutation()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            ITransaction txn = new Transaction(client);
            await txn.Commit();

            await client.DidNotReceive().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>());
        }

        [Test]
        public async Task Commit_ClientReceivedCommitIfMutation()
        {
            (var client, _) = MinimalClient();

            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);
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

            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>()).Returns(
                    Result.Fail(new ExceptionalError(
                        new RpcException(new Status(), "Something failed"))));
            var result = await txn.Commit();

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

    }
}

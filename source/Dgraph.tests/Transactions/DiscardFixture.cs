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
    public class DiscardFixture : TransactionFixtureBase
    {

        [Test]
        public async Task Discard_SetsTransactionStateToAborted()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            ITransaction txn = new Transaction(client);
            await txn.Discard();

            txn.TransactionState.Should().Be(TransactionState.Aborted);
        }

        [Test]
        public async Task Discard_ClientNotReceiveDiscardIfNoMutation()
        {
            var client = Substitute.For<IDgraphClientInternal>();
            ITransaction txn = new Transaction(client);
            await txn.Discard();

            await client.DidNotReceive().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>());
        }

        [Test]
        public async Task Discard_ClientReceivedDiscardIfMutation()
        {
            (var client, _) = MinimalClient();

            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);
            await txn.Discard();

            await client.Received().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>());
        }

        [Test]
        public async Task Discard_FailsOnException()
        {
            (var client, _) = MinimalClient();

            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result>>>(),
                Arg.Any<Func<RpcException, Result>>()).Returns(
                    Result.Fail(new ExceptionalError(new RpcException(new Status(), "Something failed"))));
            var result = await txn.Discard();

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
            txn.TransactionState.Should().Be(TransactionState.Aborted);
        }

    }
}

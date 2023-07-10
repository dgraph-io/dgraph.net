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
            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder();
            await txn.Do(req);

            await client.DidNotReceive().DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>());
            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        [Test]
        public async Task Mutate_CommitNowChangesStateToCommitted()
        {
            (var client, _) = MinimalClient();
            ITransaction txn = new Transaction(client);

            var mu = new MutationBuilder().CommitNow().SetJson("json");
            await txn.Mutate(mu);

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

            ITransaction txn = new Transaction(client);

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Result.Fail(new ExceptionalError(new RpcException(new Status(), "Something failed"))));

            var result = await txn.Do(req);

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public async Task Mutate_PassesBackResult()
        {
            (var client, var assigned) = MinimalClient();
            ITransaction txn = new Transaction(client);

            var response = new Api.Response() { Json = ByteString.CopyFromUtf8("json") };
            response.Uids.Add(new Dictionary<string, string> { { "node1", "0x1" } });

            client.DgraphExecute(
                Arg.Any<Func<Api.Dgraph.DgraphClient, Task<Result<Response>>>>(),
                Arg.Any<Func<RpcException, Result<Response>>>()).Returns(
                    Result.Ok(new Response(response)));

            var req = new RequestBuilder().
                WithMutations(new MutationBuilder().SetJson("json"));
            await txn.Do(req);
            var result = await txn.Do(req);

            result.IsSuccess.Should().BeTrue();
            result.Value.DgraphResponse.Should().BeEquivalentTo(response);
        }

    }
}

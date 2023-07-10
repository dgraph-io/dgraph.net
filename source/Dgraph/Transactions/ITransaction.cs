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

using FluentResults;
using Grpc.Core;

namespace Dgraph.Transactions
{
    /// <summary>
    /// A single atomic transaction.
    /// A transaction lifecycle is as follows:
    ///  1. Created using IDgraphClient.NewTransaction()
    ///  2. Various Query and Mutate calls made.
    ///  3. Commit or Discard used. If any mutations have been made, It's important
    ///     that at least one of these methods is called to clean up resources. Discard
    ///     is a no-op if Commit has already been called.
    /// </summary>
    public interface ITransaction : IQuery, IDisposable
    {
        /// <summary>
        /// Mutate allows data stored on Dgraph instances to be modified.
        /// The fields in api.Mutation come in pairs, set and delete.
        /// Mutations can either be encoded as JSON or as RDFs.
        ///
        /// If CommitNow is set, then this call will result in the transaction
        /// being committed. In this case, an explicit call to Commit doesn't
        /// need to be made subsequently.
        ///
        /// If the mutation fails, then the transaction is discarded and all
        /// future operations on it will fail.
        /// </summary>
        Task<Result<Response>> Mutate(Api.Mutation mutation, CallOptions? options = null)
        {
            var request = new Api.Request();
            request.Mutations.Add(mutation);
            request.CommitNow = mutation.CommitNow;
            return Do(request, options);
        }

        /// <summary>
        /// Mutate allows data stored on Dgraph instances to be modified.
        /// The fields in api.Mutation come in pairs, set and delete.
        /// Mutations can either be encoded as JSON or as RDFs.
        ///
        /// If CommitNow is set, then this call will result in the transaction
        /// being committed. In this case, an explicit call to Commit doesn't
        /// need to be made subsequently.
        ///
        /// If the mutation fails, then the transaction is discarded and all
        /// future operations on it will fail.
        /// </summary>
        Task<Result<Response>> Mutate(MutationBuilder mutation, CallOptions? options = null)
        {
            return Mutate(mutation.Mutation, options);
        }

        /// <summary>
        /// Execute a query followed by one or more mutations.
        /// </summary>
        Task<Result<Response>> Do(RequestBuilder request, CallOptions? options = null)
        {
            return Do(request.Request, options);
        }

        /// <summary>
        /// Execute a query followed by one or more mutations.
        /// </summary>
        Task<Result<Response>> Do(Api.Request request, CallOptions? options = null);

        /// <summary>
        /// Discard the transaction. Any effects are discarded from Dgraph
        /// and the transaction can't be used again.
        /// </summary>
        Task<Result> Discard(CallOptions? options = null);

        /// <summary>
        /// Commit the transaction. IF successful, any mutations in this
        /// transaction are committed in Dgraph. The transaction can't be 
        /// used again after a call to Commit.
        /// </summary>
        Task<Result> Commit(CallOptions? options = null);
    }
}

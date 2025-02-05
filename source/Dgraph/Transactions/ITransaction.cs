/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
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
        /// Run a mutation using a <see cref="MutationBuilder"/>. 
        /// Otherwise identical to Mutate(<see cref="Api.Mutation"/>).
        /// </summary>
        Task<Result<Response>> Mutate(MutationBuilder mutation, CallOptions? options = null)
        {
            return Mutate(mutation.Mutation, options);
        }

        /// <summary>
        /// Execute a query using a <see cref="RequestBuilder"/>.
        /// Otherwise identical to Do(<see cref="Api.Request"/>).
        /// </summary>
        Task<Result<Response>> Do(RequestBuilder request, CallOptions? options = null)
        {
            return Do(request.Request, options);
        }

        /// <summary>
        /// Execute a query followed by one or more mutations.
        ///
        /// If CommitNow is set, then this call will result in the transaction
        /// being committed. In this case, an explicit call to Commit doesn't
        /// need to be made subsequently.
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

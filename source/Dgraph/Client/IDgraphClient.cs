/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Dgraph.Transactions;
using FluentResults;
using Grpc.Core;

namespace Dgraph
{
    /// <summary>
    /// An IDgraphClient is connected to a Dgraph cluster (to one or more Alpha
    /// nodes).  Once a client is made, the client manages the connections and 
    // shuts all connections down on exit.
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">Thrown if the client
    /// has been disposed and calls are made.</exception>
    public interface IDgraphClient : IDisposable
    {
        /// <summary>
        /// Log in the client to the default namespace (0) using the provided
        /// credentials. Valid for the duration the client is alive.
        /// </summary>
        Task<Result> Login(string user, string password, CallOptions? options = null)
        {
            return LoginIntoNamespace(user, password, 0, options);
        }

        /// <summary>
        /// Log in the client to the provided namespace using the provided
        /// credentials. Valid for the duration the client is alive.
        /// </summary>
        Task<Result> LoginIntoNamespace(string user, string password, ulong ns, CallOptions? options = null);

        /// <summary>
        /// Alter the Dgraph database (alter schema, drop everything, etc.).
        /// </summary>
        Task<Result> Alter(Api.Operation op, CallOptions? options = null);

        /// <summary>
        /// Create a transaction that can run queries and mutations.
        /// </summary>
        ITransaction NewTransaction();

        /// <summary>
        /// Create a transaction that can only query.  
        ///
        /// Read-only transactions circumvent the usual consensus protocol
        /// and so can increase read speed. 
        ///
        /// Best effort tells Dgraph to use transaction time stamps from
        /// memory on best-effort basis to reduce the number of outbound 
        /// requests to Zero. This may yield improved latencies in read-bound 
        /// workloads where linearizable reads are not strictly needed.
        /// </summary>
        IQuery NewReadOnlyTransaction(Boolean bestEffort = false);

        /// <summary>
        /// Returns the Dgraph version string.
        /// </summary>
        Task<Result<string>> CheckVersion(CallOptions? options = null);
    }
}

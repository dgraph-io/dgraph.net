/*
 * Copyright 2020 Dgraph Labs, Inc. and Contributors
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
        /// Alter the Dgraph database (alter schema, drop everything, etc.).
        /// </summary>
        Task<FluentResults.Result> Alter(Api.Operation op, CallOptions? options = null);

        /// <summary>
        /// Returns the Dgraph version string.
        /// </summary>
        Task<FluentResults.Result<string>> CheckVersion(CallOptions? options = null);

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
        /// Create a transaction that can run queries and mutations.
        /// </summary>
        ITransaction NewTransaction();

    }
}
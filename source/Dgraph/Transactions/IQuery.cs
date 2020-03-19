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
 
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;

namespace Dgraph.Transactions {

    public enum TransactionState { OK, Committed, Aborted, Error }

    /// <summary>
    /// Represents read-only 'transactions'.  Unlike ITransactions,
    /// there's no need to discard, so use like:
    ///
    /// <code>  
    /// var ro = client.NewReadOnlyTransaction()
    /// var resp = ro.Query(...)
    /// </code>
    /// </summary>
    public interface IQuery {

        TransactionState TransactionState { get; }

        /// <summary>
        /// Run a query.
        /// </summary>
        Task<FluentResults.Result<Api.Response>> Query(
            string queryString, 
            CallOptions? options = null
        );

        /// <summary>
        /// Run a query with variables.
        /// </summary>
        Task<FluentResults.Result<Api.Response>> QueryWithVars(
            string queryString, 
            Dictionary<string, string> varMap,
            CallOptions? options = null
        );

    }
    
}
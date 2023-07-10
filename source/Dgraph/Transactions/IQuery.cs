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
    public enum TransactionState
    {
        OK,
        Committed,
        Aborted,
        Error
    }

    /// <summary>
    /// A read-only transaction that cannot commit mutations.
    /// </summary>
    public interface IQuery
    {
        TransactionState TransactionState { get; }

        /// <summary>
        /// Run a query and return a JSON response.
        /// </summary>
        Task<Result<Response>> Query(string queryString, CallOptions? options = null)
        {
            return QueryWithVars(queryString, new Dictionary<string, string>(), options);
        }

        /// <summary>
        /// Run a query with variables and return a JSON response.
        /// </summary>
        Task<Result<Response>> QueryWithVars(
            string queryString,
            Dictionary<string, string> varMap,
            CallOptions? options = null
        );

        /// <summary>
        /// Run a query with variables and return a RDF response.
        /// </summary>
        Task<Result<Response>> QueryRDF(string queryString, CallOptions? options = null)
        {
            return QueryRDFWithVars(queryString, new Dictionary<string, string>(), options);
        }

        /// <summary>
        /// Run a query and return a RDF response.
        /// </summary>
        Task<Result<Response>> QueryRDFWithVars(
            string queryString,
            Dictionary<string, string> varMap,
            CallOptions? options = null
        );
    }
}

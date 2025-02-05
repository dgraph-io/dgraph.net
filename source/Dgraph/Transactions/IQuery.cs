/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
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

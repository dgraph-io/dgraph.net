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
using Grpc.Core;

namespace DgraphDotNet.Transactions
{

    /// <summary>
    /// Use transactions like :
    ///     
    /// <code>  
    /// using(var txn = client.NewTransaction()) {
    ///    txn.mutate
    ///    txn.query
    ///    txn.mutate
    ///    txn.commit
    /// } 
    /// </code>
    ///
    /// or
    ///
    /// <code>  
    /// var txn = client.NewTransaction()
    /// txn.mutate()
    /// if(...) {
    ///    txn.commit();
    /// } else {
    ///    txn.discard();
    /// }
    /// </code>
    /// </summary>
    public interface ITransaction : IQuery, IDisposable {
    
        /// <summary>
        /// Run a mutation.  If CommitNow is set on the request, then
        /// the transaction will commit and can't be used again.
        /// </summary>
        Task<FluentResults.Result<Api.Response>> Mutate(
            RequestBuilder request,
            CallOptions? options = null    
        );

        /// <summary>
        /// Discard the transaction.  Any effects are discarded from Dgraph
        /// and the transaction can't be used again.
        /// </summary>
        Task<FluentResults.Result> Discard(CallOptions? options = null);
        
        /// <summary>
        /// Commit the transaction.  IF successful, any mutations in this
        /// transaction are committed in Dgraph.  The transaction can't be 
        /// used again after a call to Commit.
        /// </summary>
        Task<FluentResults.Result> Commit(CallOptions? options = null);

    }

}
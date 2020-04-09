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
using System.Collections.Generic;
using Api;
using Dgraph.Transactions;
using FluentResults;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;

// For unit testing.  Allows to make mocks of the internal interfaces and factories
// so can test in isolation from a Dgraph instance.
//
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dgraph.tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")] // for NSubstitute

namespace Dgraph {

    public class DgraphClient : IDgraphClient, IDgraphClientInternal {

        private readonly List<Api.Dgraph.DgraphClient> dgraphs = 
            new List<Api.Dgraph.DgraphClient>();

        public DgraphClient(params GrpcChannel[] channels) {
            foreach (var chan in channels) {
                Api.Dgraph.DgraphClient client = new Api.Dgraph.DgraphClient(chan);
                dgraphs.Add(client);
            }
        }

        // 
        // ------------------------------------------------------
        //              Transactions
        // ------------------------------------------------------
        //
        #region transactions

        public ITransaction NewTransaction() {
            AssertNotDisposed();

            return new Transaction(this);
        }

        public IQuery NewReadOnlyTransaction(Boolean bestEffort = false) {
            AssertNotDisposed();

            return new ReadOnlyTransaction(this, bestEffort);
        }

        #endregion

        // 
        // ------------------------------------------------------
        //              Execution
        // ------------------------------------------------------
        //
        #region execution

        private int NextConnection = 0;
        private int GetNextConnection() {
			var next = NextConnection;
			NextConnection = (next  + 1) % dgraphs.Count;
            return next;
        }			

        public async Task<FluentResults.Result> Alter(Api.Operation op) {
            return await DgraphExecute(
                async (dg) => {
                    await dg.AlterAsync(op);
                    return Results.Ok();
                },
                (rpcEx) => Results.Fail(new FluentResults.ExceptionalError(rpcEx))
            );
        }

        public async Task<FluentResults.Result<string>> CheckVersion() {
            return await DgraphExecute(
                async (dg) => {
                    var versionResult = await dg.CheckVersionAsync(new Check());
                    return Results.Ok<string>(versionResult.Tag);;
                },
                (rpcEx) => Results.Fail<string>(new FluentResults.ExceptionalError(rpcEx))
            );
        }

        public async Task<T> DgraphExecute<T>(
            Func<Api.Dgraph.DgraphClient, Task<T>> execute, 
            Func<RpcException, T> onFail
        ) {

            AssertNotDisposed();

            try {
                return await execute(dgraphs[GetNextConnection()]);
            } catch (RpcException rpcEx) {
                return onFail(rpcEx);
            }
        }

        #endregion

        // 
        // ------------------------------------------------------
        //              Disposable Pattern
        // ------------------------------------------------------
        //
        #region disposable pattern

        // see disposable pattern at : https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
        // and http://reedcopsey.com/tag/idisposable/
        //
        // Trying to follow the rules here 
        // https://blog.stephencleary.com/2009/08/second-rule-of-implementing-idisposable.html
        // for all the dgraph dispose bits
        //
        // For this class, it has only managed IDisposable resources, so it just needs to call the Dispose()
        // of those resources.  It's safe to have nothing else, because IDisposable.Dispose() must be safe to call
        // multiple times.  Also don't need a finalizer.  So this simplifies the general pattern, which isn't needed here.

        bool disposed; // = false;
        protected bool Disposed => disposed;

        protected void AssertNotDisposed() {
            if (Disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {
            DisposeIDisposables();
        }

        protected virtual void DisposeIDisposables() {
            if (!Disposed) {
                this.disposed = true;  
                foreach (var dgraph in dgraphs) {
                    // FIXME:
                    // can't get to the chans??
                    // dgraph. Dispose();
                }
            }
        }

        #endregion
    }
}
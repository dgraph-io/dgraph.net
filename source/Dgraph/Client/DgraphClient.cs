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
using Dgraph.Transactions;
using FluentResults;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;
using System.Threading;

// For unit testing.  Allows to make mocks of the internal interfaces and factories
// so can test in isolation from a Dgraph instance.
//
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dgraph.tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")] // for NSubstitute

namespace Dgraph {

    public class DgraphClient : IDgraphClient, IDgraphClientInternal {

        private readonly List<IDgraph> _dgraphs = new List<IDgraph>();

        public Api.Jwt Jwt { get; set; }

        public DgraphClient(params GrpcChannel[] channels) : this(new DgraphFactory(), channels) {

        }

        internal DgraphClient(IDgraphFactory clientFactory, params GrpcChannel[] channels) {
            foreach (var chan in channels) {
                _dgraphs.Add(clientFactory.FromChannel(chan));
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

        private int _nextConnection = 0;
        private IDgraph nextDgraph() {
			var next = _nextConnection;
			_nextConnection = (next  + 1) % _dgraphs.Count;
            return _dgraphs[next];
        }			

        private CallOptions jwtOptions(CallOptions? options = null) {

            var opts = options ?? new CallOptions(null, null, default(CancellationToken));

            if(Jwt != null) { 
                var md = new Metadata();
                md.Add("accessJwt", Jwt.AccessJwt);
                opts = opts.WithHeaders(md);
            }

            return opts;
        }

        private bool jwtIsExpired(ResultBase result) {
            if(result.IsSuccess) {
                return false;
            }   

            if(result.Errors.Count > 0 && 
                result.Errors[0] is ExceptionalError ex &&
                ex.Exception is RpcException rpcEx &&
                rpcEx.StatusCode == StatusCode.Unauthenticated) {
                    return true;
            }

            return false;
        }

        private async Task<T> retry<T>(
            Func<CallOptions, Task<T>> dgFunc, 
            CallOptions? options = null) where T : ResultBase<T> {

            AssertNotDisposed();

            var result = await dgFunc(jwtOptions(options));

            if(jwtIsExpired(result)) {
                var refresh = await RefreshLogin(options);
                if(refresh.IsFailed) {
                    result.WithReasons(refresh.Reasons);
                    
                }

                return await dgFunc(jwtOptions(options));
            }

            return result;
        }

        public async Task<Result> Alter(Api.Operation operation, CallOptions? options = null) =>
            await retry(
                async (opts) => await nextDgraph().AlterAsync(operation, opts),
                options);        

        public async Task<Result<string>> CheckVersion(CallOptions? options = null) =>
            await retry(
                async (opts) => await nextDgraph().CheckVersionAsync(opts),
                options);

        public async Task<Result<Response>> QueryAsync(
            Api.Request request, 
            CallOptions? options = null
        ) => await retry(
                async (opts) => await nextDgraph().QueryAsync(request, opts),
                options);

        public async Task<Result> CommitOrAbortAsync(
            Api.TxnContext context,
            CallOptions? options = null
        ) => await retry(
                async (opts) => await nextDgraph().CommitOrAbortAsync(context, opts),
                options);

        #endregion

        // 
        // ------------------------------------------------------
        //              Enterprise
        // ------------------------------------------------------
        //
        #region enterprise

        public async Task<Result> Login(
            string username, 
            string password,
            CallOptions? options = null
        ) {
            AssertNotDisposed();

            var jwtResult = await nextDgraph().LoginAsync(
                new Api.LoginRequest() { Userid = username, Password = password },
                options ?? new CallOptions(null, null, default(CancellationToken)));
        
            Jwt = jwtResult.ValueOrDefault;
            return jwtResult.ToResult();  
        }
        
        private async Task<Result> RefreshLogin(CallOptions? options = null) {
            if(Jwt == null) {
                return Results.Fail("can't refresh - not logged in");
            }

            var jwtResult = await nextDgraph().LoginAsync(
                new Api.LoginRequest() { RefreshToken = Jwt.RefreshJwt },
                jwtOptions(options));
        
            Jwt = jwtResult.ValueOrDefault;
            return jwtResult.ToResult(); 
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

        bool _disposed; // = false;

        protected void AssertNotDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {
            DisposeIDisposables();
        }

        protected virtual void DisposeIDisposables() {
            if (!_disposed) {
                _disposed = true;  
                foreach (var dgraph in _dgraphs) {
                    dgraph.Dispose();
                }
            }
        }

        #endregion
    }
}
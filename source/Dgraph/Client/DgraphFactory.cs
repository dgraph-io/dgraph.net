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
using FluentResults;
using Grpc.Core;
using Grpc.Net.Client;

namespace Dgraph {

    // Helps to abstract Dgraph interface, so we can unit test without
    // needing to connect to a Dgraph instance.
    //
    // Also allows separating error/exception handling from from Jwt
    // etc handling to make interactions clearer.

    internal interface IDgraphFactory {
        IDgraph FromChannel(GrpcChannel channel);
    }

    internal class DgraphFactory : IDgraphFactory {
        public IDgraph FromChannel(GrpcChannel channel) {
            return new DgraphProxy(channel);
        }
    }

    internal interface IDgraph : IDisposable {
        Task<Result<Api.Jwt>> LoginAsync(Api.LoginRequest request, CallOptions options);
        Task<Result> AlterAsync(Api.Operation operation, CallOptions options);
        Task<Result<string>> CheckVersionAsync(CallOptions options);
        Task<Result<Response>> QueryAsync(Api.Request request, CallOptions options);
        Task<Result> CommitOrAbortAsync(Api.TxnContext context, CallOptions options);
    }

    internal class DgraphProxy : IDgraph {

        private Api.Dgraph.DgraphClient dgraph;
        private GrpcChannel channel;

        public DgraphProxy(GrpcChannel channel) {
            this.channel = channel;
            dgraph = new Api.Dgraph.DgraphClient(channel);
        }

        public async Task<Result<Api.Jwt>> LoginAsync(Api.LoginRequest request, CallOptions options) {
            AssertNotDisposed();
            try {
                var loginResult = await dgraph.LoginAsync(request, options);
                return Results.Ok<Api.Jwt>(Api.Jwt.Parser.ParseFrom(loginResult.Json));
            } catch (RpcException rpcEx) {
                return Results.Fail<Api.Jwt>(new ExceptionalError(rpcEx));
            }
        }

        public async Task<Result> AlterAsync(Api.Operation operation, CallOptions options) {
            AssertNotDisposed();
            try {
                await dgraph.AlterAsync(operation, options);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail(new ExceptionalError(rpcEx));
            }
        }

        public async Task<Result<string>> CheckVersionAsync(CallOptions options) {
            AssertNotDisposed();
            try {
                var versionResult = await dgraph.CheckVersionAsync(new Api.Check(), options);
                return Results.Ok<string>(versionResult.Tag);
            } catch (RpcException rpcEx) {
                return Results.Fail<string>(new ExceptionalError(rpcEx));
            }
        }

        public async Task<Result<Response>> QueryAsync(Api.Request request, CallOptions options) {
            AssertNotDisposed();
            try {
                var versionResult = await dgraph.CheckVersionAsync(new Api.Check(), options);
                return Results.Ok<Response>(new Response(await dgraph.QueryAsync(request, options)));
            } catch (RpcException rpcEx) {
                return Results.Fail<Response>(new ExceptionalError(rpcEx));
            }
        }

        public async Task<Result> CommitOrAbortAsync(Api.TxnContext context, CallOptions options) {
            AssertNotDisposed();
            try {
                await dgraph.CommitOrAbortAsync(context, options);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail<string>(new ExceptionalError(rpcEx));
            }            
        }

        #region disposable pattern

        bool disposed; // = false;

        protected void AssertNotDisposed() {
            if (disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {
            if (!disposed) {
                this.disposed = true;  
                channel.Dispose();
            }
        }

        #endregion

    }

}
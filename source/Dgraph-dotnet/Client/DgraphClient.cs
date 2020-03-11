using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Api;
using DgraphDotNet.Schema;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using FluentResults;
using Grpc.Core;
using System.Threading.Tasks;

namespace DgraphDotNet {

    internal class DgraphClient : IDgraphClient, IDgraphClientInternal {

        protected readonly IGRPCConnectionFactory connectionFactory;
        protected readonly ITransactionFactory transactionFactory;

        internal DgraphClient(IGRPCConnectionFactory connectionFactory, ITransactionFactory transactionFactory) {
            this.connectionFactory = connectionFactory;
            this.transactionFactory = transactionFactory;
        }

		protected readonly System.Object ClientMutex = new System.Object();

        // 
        // ------------------------------------------------------
        //                   Connections
        // ------------------------------------------------------
        //

        #region Connections

        private readonly List<IGRPCConnection> connections = new List<IGRPCConnection>();

        public void Connect(
            string address, 
            ChannelCredentials credentials = null, 
            IEnumerable<ChannelOption> options = null
        ) {
            AssertNotDisposed();

            if (!string.IsNullOrEmpty(address)) {
                lock(ClientMutex) {
                    var existing = connections.FirstOrDefault(c => c.Target.Equals(address));
                    if(existing == null) {
                        if (connectionFactory.TryConnect(address, out var connection, credentials, options)) {
                            connections.Add(connection);
                        }
                    }
                }
            }
        }

        #endregion

        // 
        // ------------------------------------------------------
        //                   Transactions
        // ------------------------------------------------------
        //

        #region transactions

        private int NextConnection = 0;
        private int GetNextConnection() {
			var next = NextConnection;
			NextConnection = (next  + 1) % connections.Count;
            return next;
        }			

        public async Task<FluentResults.Result> AlterSchema(string newSchema) {
            AssertNotDisposed();

            var op = new Api.Operation();
            op.Schema = newSchema;

            try {
                await connections[GetNextConnection()].Alter(op);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result> DropAll() {
            AssertNotDisposed();

            var op = new Api.Operation() {
                DropAll = true
            };

            try {
                await connections[GetNextConnection()].Alter(op);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result<string>> CheckVersion() {
            AssertNotDisposed();

            try {
                var versionResult = await connections[GetNextConnection()].CheckVersion();
                return Results.Ok<string>(versionResult.Tag);
            } catch (RpcException rpcEx) {
                return Results.Fail<string>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result<DgraphSchema>> SchemaQuery(string schemaQuery) {
            AssertNotDisposed();
            
            if(schemaQuery == null) {
                schemaQuery = "schema { }";
            }

            using(var transaction = NewTransaction()) {
                return await transaction.SchemaQuery(schemaQuery);
            }
        }

        public async Task<FluentResults.Result<string>> Query(string queryString) {
            AssertNotDisposed();

            return await QueryWithVars(queryString, new Dictionary<string, string>());
        }

        public async Task<FluentResults.Result<string>> QueryWithVars(string queryString, Dictionary<string, string> varMap) {
            AssertNotDisposed();

            using(var transaction = NewTransaction()) {
                return await transaction.QueryWithVars(queryString, varMap);
            }
        }

        public ITransaction NewTransaction() {
            AssertNotDisposed();

            return transactionFactory.NewTransaction(this);
        }

        public async Task<Response> Query(Api.Request req) {
            AssertNotDisposed();

            return await connections[GetNextConnection()].Query(req);
        }

        public async Task<Response> Mutate(Api.Request mut) {
            AssertNotDisposed();

            return await connections[GetNextConnection()].Mutate(mut);
        }

        public async Task Commit(TxnContext context) {
            AssertNotDisposed();

            await connections[GetNextConnection()].Commit(context);
        }

        public async Task Discard(TxnContext context) {
            AssertNotDisposed();

            await connections[GetNextConnection()].Discard(context);
        }

        #endregion

        // 
        // ------------------------------------------------------
        //              disposable pattern.
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
                this.disposed = true; // throw ObjectDisposedException on calls to client if it has been disposed. 
                foreach (var con in connections) {
                    con.Dispose();
                }
            }
        }

        #endregion
    }
}
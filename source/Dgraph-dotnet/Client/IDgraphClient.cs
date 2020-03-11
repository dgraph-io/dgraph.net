using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DgraphDotNet.Transactions;
using Grpc.Core;

/*
 *
 *  service Dgraph {
 *	  rpc Query (Request)            returns (Response) {}
 *    rpc Alter (Operation)          returns (Payload) {}
 *    rpc CommitOrAbort (TxnContext) returns (TxnContext) {}
 *    rpc CheckVersion(Check)        returns (Version) {}
 *  }
 *
 */

namespace DgraphDotNet {

    /// <summary>
    /// A client is in charge of making connections to Dgraph backends.  Once a
    /// connection is made, the client manages the connection (and shuts it down
    /// on exit).
    ///
    /// This type of client can do transactions with query and JSON mutations.
    /// see : https://docs.dgraph.io/mutations/#json-mutation-format
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">Thrown if the client
    /// has been disposed and calls are made.</exception>
    public interface IDgraphClient : IDisposable, IQuery {

        /// <summary>
        /// Connect to a backend Dgraph instance.  Multiple connections can be
        /// made in a single client.
        /// </summary>
        /// <param name="address"> address to connect to: e.g. of the form
        /// 127.0.0.1:9080.</param>
        /// <remarks>All addresses added to a single client should be addresses
        /// of servers in a single Dgraph cluster.   On running a call such as
        /// <see cref="AlterSchema(string)"/>
        /// or submitting an <see cref="ITransaction"/> any one of the
        /// connections is used.
        /// </remarks>
        void Connect(string address, ChannelCredentials credentials = null, IEnumerable<ChannelOption> options = null);

        /// <summary>
        /// Alter the schema see: https://docs.dgraph.io/query-language/#schema
        /// </summary>
        Task<FluentResults.Result> AlterSchema(string newSchema);

        /// <summary>
        /// Remove everything from the database.
        /// </summary>
        Task<FluentResults.Result> DropAll();

        /// <summary>
        /// Returns the Dgraph version string.
        /// </summary>
        Task<FluentResults.Result<string>> CheckVersion();

        ITransaction NewTransaction();
    }
}
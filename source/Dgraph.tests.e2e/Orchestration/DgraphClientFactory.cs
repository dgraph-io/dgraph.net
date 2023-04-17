using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Serilog;

namespace Dgraph.tests.e2e.Orchestration
{
    public class DgraphClientFactory
    {

        private bool printed;

        public async Task<IDgraphClient> GetDgraphClient()
        {

            // FIXME: This is not what you'd want to do in a real app.  Normally, there
            // would be tls to the server.  TO ADD - tests of running over https, and
            // with a Dgraph tls client certificate, and in enterprise mode.
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var client = new DgraphClient(GrpcChannel.ForAddress("http://127.0.0.1:9080"));

            if (!printed)
            {
                var result = await client.CheckVersion();
                if (result.IsSuccess)
                {
                    Log.Information("Connected to Dgraph version {Version}", result.Value);
                }
                else
                {
                    Log.Information("Failed to get Dgraph version {Error}", result);
                }
                printed = true;
            }

            return client;
        }

    }
}
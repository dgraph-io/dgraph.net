using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Serilog;

namespace Dgraph.tests.e2e.Orchestration
{
    class InjectedDgraphClientFactory : IDgraphClientFactory
    {
        private bool printed;

        private readonly IServiceProvider provider;

        public InjectedDgraphClientFactory(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public async Task<IDgraphClient> GetDgraphClient()
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            DgraphClient client = (DgraphClient)provider.GetService(typeof(DgraphClient));

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
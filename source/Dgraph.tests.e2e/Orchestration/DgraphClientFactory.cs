using System;
using System.Linq;
using System.Threading.Tasks;
using Dgraph.tests.e2e.Errors;
using FluentResults;
using Grpc.Net.Client;
using Serilog;

namespace Dgraph.tests.e2e.Orchestration
{
    public class DgraphClientFactory {

        private bool printed;

        private readonly DgraphTestSettings settings;

        public DgraphClientFactory(DgraphTestSettings settings) {
            this.settings = settings;
        }

        public async Task<IDgraphClient> GetDgraphClient(bool groot = false) {

            var client = Connect();
            var loggedIn = await Login(client, groot);
            if(loggedIn.IsFailed) {
                throw new DgraphDotNetTestFailure("Failed to login to Dgraph", loggedIn);
            }

            if(!printed) {
                var result = await client.CheckVersion();
                if (result.IsSuccess) {
                    Log.Information("Connected to Dgraph version {Version}", result.Value);
                } else {
                    Log.Information("Failed to get Dgraph version {Error}", result);
                }
                printed = true;
            }
            
            return client;
        }

        private IDgraphClient Connect() {
            if(settings.CaCert == null) {
                // FIXME: This is not what you'd want to do in a real app.  Normally, there
                // would be tls to the server.  TO ADD - tests of running over https, and
                // with a Dgraph tls client certificate, and in enterprise mode.
                AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            } else {
                // FIXME: read the tls certs ... or really set them in the verify method 
                // and put into services
            }

            return new DgraphClient(settings.Endpoints.Select(
                endpoint => GrpcChannel.ForAddress(endpoint.EndPoint)).ToArray());
        }

        private async Task<Result> Login(IDgraphClient client, bool groot) {
            if(groot) {
                return await client.Login("groot", settings.GrootPassword);
            }

            if(settings.TestUser != null) {
                return await client.Login(settings.TestUser, settings.TestUserPassword);
            }

            return Results.Ok();
        }
    }
}
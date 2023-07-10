/*
 * Copyright 2023 Dgraph Labs, Inc. and Contributors
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
            var client = DgraphClient.Create(GrpcChannel.ForAddress("http://127.0.0.1:9080"));

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

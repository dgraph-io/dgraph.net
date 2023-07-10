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

using Grpc.Core;
using Grpc.Net.Client;

namespace Dgraph
{
    public static class DgraphCloudChannel
    {
        private const string CloudPort = "443";

        /// <summary>
        /// Create a new TLS connection to a Dgraph Cloud backend.
        /// </summary>
        /// <returns>A new instance of <see cref="GrpcChannel"/></returns>
        /// <exception cref="ArgumentException"></exception>
        public static GrpcChannel Create(string address, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException($"Invalid address to Dgraph Cloud: {address}");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("Invalid api key for Dgraph Cloud");
            }

            string grpcUri = address;
            if (!grpcUri.StartsWith("http"))
            {
                grpcUri = $"https://{grpcUri}";
            }
            if (Uri.TryCreate(grpcUri, UriKind.Absolute, out Uri u) && u.Host.Contains("."))
            {
                if (u.Host.Contains(".grpc."))
                {
                    grpcUri = $"https://{u.Host}:{CloudPort}";
                }
                else
                {
                    var uriParts = u.Host.Split(".", 2);
                    grpcUri = $"https://{uriParts[0]}.grpc.{uriParts[1]}:{CloudPort}";
                }
            }
            else
            {
                throw new ArgumentException($"Invalid address to Dgraph Cloud: {address}");
            }

            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("DG-Auth", apiKey);
                return Task.CompletedTask;
            });

            // SslCredentials is used here because this channel is using TLS.
            // CallCredentials can't be used with ChannelCredentials.Insecure on non-TLS channels.
            return GrpcChannel.ForAddress(grpcUri, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
        }
    }
}

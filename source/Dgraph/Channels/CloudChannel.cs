
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;

namespace Dgraph
{
    public static class DgraphCloudChannel
    {
        public static GrpcChannel Create(string address, string apiKey)
        {
            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                if (!string.IsNullOrEmpty(apiKey))
                {
                    metadata.Add("DG-Auth", apiKey);
                }
                return Task.CompletedTask;
            });

            // SslCredentials is used here because this channel is using TLS.
            // CallCredentials can't be used with ChannelCredentials.Insecure on non-TLS channels.
            var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
            return channel;
        }
    }
}
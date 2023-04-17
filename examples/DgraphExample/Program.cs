using Dgraph;

namespace DgraphExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Connect to Dgraph Cloud.
            var cloudUrl = "https://green-bird.grpc.us-east-1.aws.cloud.dgraph.io/graphql";
            var APIKEY = "MjM2MjA5YTYzMmViODJjYTA2NWM4YmVhY2EzZDgzNTE=";
            using var client = new DgraphClient(SlashChannel.Create(cloudUrl, APIKEY));

            var version = await client.CheckVersion();

            if (version.IsSuccess)
            {
                Console.WriteLine("Version: " + version.Value);
            }

            // Perform a query.
            string query = @"schema{}";

            // Cria uma transação retryable
            using var txn = client.NewRetryableTransaction();

            var response = await txn.Query(query);

            if (response.IsSuccess)
            {
                Console.WriteLine("Query response: " + response.Value.Json);
            }
            else
            {
                Console.WriteLine("Error: " + response.Errors.First().Message);
            }

        }
    }
}

using Dgraph;

namespace DgraphExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Connect to Dgraph Cloud.
            var cloudUrl = "https://green-bird.grpc.us-east-1.aws.cloud.dgraph.io/graphql";
            var APIKEY = "=";
            using var client = new DgraphClient(SlashChannel.Create(cloudUrl, APIKEY));

            var version = await client.CheckVersion();

            if (version.IsSuccess)
            {
                Console.WriteLine("Version: " + version.Value);
            }

            string query = @"schema{}";

            string result = await ExecuteDQL.Query(client, query);

            // Print the result
            Console.WriteLine(result);

        }

    }
}

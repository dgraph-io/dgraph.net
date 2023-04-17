using Dgraph;

namespace DgraphExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Connect to Dgraph Cloud.
            var cloudUrl = "https://green-bird.grpc.us-east-1.aws.cloud.dgraph.io/graphql";
            var APIKEY = "xxx=";
            var client = new DgraphClient(SlashChannel.Create(cloudUrl, APIKEY));

            var version = await client.CheckVersion();

            Console.WriteLine("Version: " + version.Value);

            // Perform a query.
            string query = @"schema{}";

            var readOnlyTransaction = client.NewTransaction();
            var response = await readOnlyTransaction.Query(query);
            Console.WriteLine("Query response: " + response.Value.Json);

        }
    }
}

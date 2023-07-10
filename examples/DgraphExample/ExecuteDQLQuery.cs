using Dgraph;
using Dgraph.Transactions;

namespace DgraphExample
{
    class ExecuteDQL
    {
        public static async Task<string> Query(IDgraphClient Client, string query)
        {
            using (ITransaction transaction = Client.NewTransaction())
            {
                try
                {
                    // Perform a query.
                    var response = await transaction.Query(query);
                    await transaction.Commit();

                    if (response.IsFailed)
                    {
                        Console.WriteLine($"[{DateTime.Now}] gRPC response failed: {response.Errors[0].Message}");
                        return "gRPC Got error";
                    }
                    else
                    {
                        return response.Value.Json;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] An error occurred during the query: {ex.Message}");
                    await transaction.Discard();
                    return "gRPC Error during transaction";
                }
            }
        }
    }
}

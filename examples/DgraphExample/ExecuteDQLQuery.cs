using Dgraph;
using Dgraph.Transactions;

namespace DgraphExample
{
    class ExecuteDQL
    {
        public static async Task<string> Query(DgraphClient Client, string query)
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
        public static async Task<string> MutateWithRetry(DgraphClient Client, string json)
        {
            using (RetryableTransaction transaction = Client.NewRetryableTransaction())
            {
                try
                {
                    // Perform a mutation.
                    var mutation = new MutationBuilder { SetJson = json };
                    var response = await transaction.MutateWithRetry(new RequestBuilder().WithMutations(mutation));
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
                    Console.WriteLine($"[{DateTime.Now}] An error occurred during the mutation: {ex.Message}");
                    await transaction.Discard();
                    return "gRPC Error during transaction";
                }
            }
        }

        public static async Task<string> QueryWithRetry(DgraphClient Client, string query)
        {
            using (RetryableTransaction transaction = Client.NewRetryableTransaction())
            {
                try
                {
                    // Perform a query.
                    var response = await transaction.QueryWithRetry(query);
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

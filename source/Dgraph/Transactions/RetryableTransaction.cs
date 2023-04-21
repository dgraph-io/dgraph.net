using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Dgraph.Transactions
{
    public class RetryableTransaction : Transaction
    {
        public RetryableTransaction(IDgraphClientInternal client) : base(client)
        {
        }

        public async Task<FluentResults.Result<Response>> MutateWithRetry(RequestBuilder mu)
        {
            TimeSpan initialDelay = TimeSpan.FromMilliseconds(200);
            int maxRetries = 50;
            TimeSpan delay = initialDelay;
            int retries = 0;

            FluentResults.Result<Response> response = null;

            while (retries <= maxRetries)
            {
                response = await base.Mutate(mu);
                Console.WriteLine($"response : {response}");

                if (response.IsFailed)
                {
                    string errorMessage = response.Errors[0].Message;

                    if (retries == maxRetries)
                    {
                        throw new InvalidOperationException($"Mutation failed: {errorMessage}");
                    }

                    if (errorMessage.Contains("Unauthenticated"))
                    {
                        maxRetries = 5;
                        delay *= 3;
                    }
                    else if (errorMessage.Contains("Aborted") || errorMessage.Contains("Unavailable"))
                    {
                        maxRetries = 50;
                        delay *= 3;
                    }
                    else
                    {
                        maxRetries = 80;
                        delay *= 3;
                    }

                    await Task.Delay(delay);
                    retries++;
                }
                else
                {
                    return response;
                }
            }

            throw new InvalidOperationException("Should not reach this point.");
        }

        public async Task<FluentResults.Result<Response>> QueryWithRetry(string query)
        {
            TimeSpan initialDelay = TimeSpan.FromMilliseconds(200);
            int maxRetries = 50;
            TimeSpan delay = initialDelay;
            int retries = 0;

            FluentResults.Result<Response> response = null;

            while (retries <= maxRetries)
            {
                response = await base.Query(query);
                Console.WriteLine($"response : {response}");

                if (response.IsFailed)
                {
                    string errorMessage = response.Errors[0].Message;

                    if (retries == maxRetries)
                    {
                        throw new InvalidOperationException($"Query failed: {errorMessage}");
                    }

                    if (errorMessage.Contains("Unauthenticated"))
                    {
                        maxRetries = 5;
                        delay *= 3;
                    }
                    else if (errorMessage.Contains("Aborted") || errorMessage.Contains("Unavailable"))
                    {
                        maxRetries = 50;
                        delay *= 3;
                    }
                    else
                    {
                        maxRetries = 80;
                        delay *= 3;
                    }

                    await Task.Delay(delay);
                    retries++;
                }
                else
                {
                    return response;
                }
            }

            throw new InvalidOperationException("Should not reach this point.");
        }
    }
}

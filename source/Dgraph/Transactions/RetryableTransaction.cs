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

        public async Task<FluentResults.Result<Response>> MutateWithRetryAsync(RequestBuilder mu)
        {
            TimeSpan initialDelay = TimeSpan.FromMilliseconds(100);
            int maxRetries = 5;
            TimeSpan delay = initialDelay;
            int retries = 0;

            while (retries <= maxRetries)
            {
                try
                {
                    return await base.Mutate(mu);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Aborted || ex.StatusCode == StatusCode.Unavailable)
                {
                    if (retries == maxRetries)
                    {
                        throw;
                    }
                    await Task.Delay(delay);
                    delay *= 2;
                    retries++;
                }
            }

            throw new InvalidOperationException("Should not reach this point.");
        }
        public async Task<FluentResults.Result<Response>> QueryWithRetryAsync(string query)
        {
            TimeSpan initialDelay = TimeSpan.FromMilliseconds(100);
            int maxRetries = 5;
            TimeSpan delay = initialDelay;
            int retries = 0;

            while (retries <= maxRetries)
            {
                try
                {
                    await base.Query(query);
                }
                catch (Exception ex)
                {
                    if (ex is RpcException rpcEx && (rpcEx.StatusCode == StatusCode.Aborted || rpcEx.StatusCode == StatusCode.Unavailable || rpcEx.StatusCode == StatusCode.Unauthenticated))
                    {
                        if (retries == maxRetries)
                        {
                            throw;
                        }
                        await Task.Delay(delay);
                        delay *= 2;
                        retries++;
                    }
                    else
                    {
                        Console.WriteLine($"gRPC Error: {ex}");
                        throw;
                    }
                }
            }

            throw new InvalidOperationException("Should not reach this point.");
        }

    }
}
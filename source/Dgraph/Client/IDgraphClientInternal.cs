/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Grpc.Core;

namespace Dgraph
{
    /// <summary>
    /// Internal dealings of clients with Dgraph --- Not part of the
    /// external interface
    /// </summary>
    internal interface IDgraphClientInternal
    {
        Task<T> DgraphExecute<T>(
            Func<Api.Dgraph.DgraphClient, Task<T>> execute,
            Func<RpcException, T> onFail
        );
    }
}

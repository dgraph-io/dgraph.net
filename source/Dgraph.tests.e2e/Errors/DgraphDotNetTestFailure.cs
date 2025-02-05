/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using FluentResults;

namespace Dgraph.tests.e2e.Errors
{
    public class DgraphDotNetTestFailure : Exception
    {
        public readonly ResultBase FailureReason;

        public DgraphDotNetTestFailure(string message) : base(message) { }

        public DgraphDotNetTestFailure(string message, ResultBase failureReason) : base(message)
        {
            FailureReason = failureReason;
        }
    }
}

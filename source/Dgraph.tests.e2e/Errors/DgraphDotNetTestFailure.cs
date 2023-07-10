/*
 * Copyright 2023 Dgraph Labs, Inc. and Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
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

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

namespace Dgraph.Transactions
{
    public class TransactionNotOK : Error
    {
        internal TransactionNotOK(string state)
            : base("Cannot perform action when transaction is in state " + state) { }
    }

    public class TransactionReadOnly : Error
    {
        internal TransactionReadOnly() : base("Readonly transaction cannot run mutations or be committed") { }
    }

    public class StartTsMismatch : Error
    {
        internal StartTsMismatch() : base("StartTs mismatch") { }
    }
}

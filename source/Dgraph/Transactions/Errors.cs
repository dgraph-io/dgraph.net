/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
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

    public class TransactionMalformed : Error
    {
        internal TransactionMalformed(string message) : base(message) { }
    }

    public class StartTsMismatch : Error
    {
        internal StartTsMismatch() : base("StartTs mismatch") { }
    }
}

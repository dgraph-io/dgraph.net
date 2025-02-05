/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Google.Protobuf;

namespace Dgraph
{
    public class MutationBuilder
    {
        internal Api.Mutation Mutation = new Api.Mutation();

        public MutationBuilder SetJson(string setJson)
        {
            Mutation.SetJson = ByteString.CopyFromUtf8(setJson ?? "");
            return this;
        }

        public MutationBuilder SetNquads(string setNquads)
        {
            Mutation.SetNquads = ByteString.CopyFromUtf8(setNquads ?? "");
            return this;
        }

        public MutationBuilder DeleteJson(string deleteJson)
        {
            Mutation.DeleteJson = ByteString.CopyFromUtf8(deleteJson ?? "");
            return this;
        }

        public MutationBuilder DelNquads(string delNquads)
        {
            Mutation.DelNquads = ByteString.CopyFromUtf8(delNquads ?? "");
            return this;
        }

        public MutationBuilder Cond(string cond)
        {
            Mutation.Cond = cond ?? "";
            return this;
        }

        public MutationBuilder CommitNow(bool commitNow = true)
        {
            Mutation.CommitNow = commitNow;
            return this;
        }
    }
}

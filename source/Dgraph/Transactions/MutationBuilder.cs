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

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

namespace Dgraph
{
    public class RequestBuilder
    {
        internal Api.Request Request = new Api.Request();

        public RequestBuilder WithQuery(string query)
        {
            Request.Query = query;
            return this;
        }

        public RequestBuilder WithVars(Dictionary<string, string> varMap)
        {
            if (varMap is not null)
            {
                Request.Vars.Add(varMap);
            }
            return this;
        }

        public RequestBuilder WithMutations(params MutationBuilder[] mutations)
        {
            Request.Mutations.Add(mutations.Select(m => m.Mutation));
            return this;
        }

        public RequestBuilder WithMutations(params Api.Mutation[] mutations)
        {
            Request.Mutations.Add(mutations);
            return this;
        }

        public RequestBuilder CommitNow(bool commitNow = true)
        {
            Request.CommitNow = commitNow;
            return this;
        }
    }
}

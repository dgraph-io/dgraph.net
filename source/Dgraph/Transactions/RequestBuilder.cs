/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
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

/*
 * Copyright 2020 Dgraph Labs, Inc. and Contributors
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

using System.Linq;
using Api;

namespace Dgraph.Transactions
{

    public class RequestBuilder
    {

        internal Request Request = new Request();

        public string Query
        {
            get
            {
                return Request.Query;
            }
            set
            {
                Request.Query = value;
            }
        }

        public bool CommitNow
        {
            get
            {
                return Request.CommitNow;
            }
            set
            {
                Request.CommitNow = value;
            }
        }

        public RequestBuilder WithMutations(params MutationBuilder[] mutations)
        {
            Request.Mutations.Add(mutations.Select(mb => mb.Mutation));
            return this;
        }

    }
}
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

namespace Dgraph.Schema
{
    public class DgraphSchema
    {
        public List<DrgaphPredicate> Schema { get; set; }

        public List<DgraphType> Types { get; set; }

        public override string ToString()
        {
            var preds = string.Join("\n", Schema.Select(p => p.ToString()));

            var types = string.Join("\n\n",
                Types?.Select(t => t.ToString()) ?? new List<string>());

            return preds + (types.Count() > 0 ? "\n" + types + "\n" : "\n");
        }
    }
}

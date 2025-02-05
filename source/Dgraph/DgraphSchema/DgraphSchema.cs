/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
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

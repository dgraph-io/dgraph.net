/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Schema
{
    public class DgraphType
    {
        public string Name { get; set; }

        public List<DgraphField> Fields { get; set; }

        public override string ToString() =>
            "type " + Name + " {\n" +
            String.Join("\n", Fields.Select(f => "\t" + f.ToString())) + "\n" +
            "}";
    }
}

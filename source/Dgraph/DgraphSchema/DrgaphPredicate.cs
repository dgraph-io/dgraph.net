/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Schema
{
    public class DrgaphPredicate
    {
        public string Predicate { get; set; }
        public string Type { get; set; }
        public bool Index { get; set; }
        public List<string> Tokenizer { get; set; }
        public bool Reverse { get; set; }
        public bool Count { get; set; }
        public bool List { get; set; }
        public bool Upsert { get; set; }
        public bool Lang { get; set; }

        public override string ToString()
        {
            string indexFragment = "";
            if (Index)
            {
                indexFragment = "@index(" + String.Join(",", Tokenizer) + ") ";
            }
            var reverseFragment = Reverse ? "@reverse " : "";
            var countableFragment = Count ? "@count " : "";
            var typeFragment = List ? $"[{Type}]" : $"{Type}";
            var upsertFragment = Upsert ? "@upsert " : "";
            var langtagsFragment = Lang ? "@lang " : "";

            return $"{Predicate}: {typeFragment} {indexFragment}{reverseFragment}{countableFragment}{upsertFragment}{langtagsFragment}.";
        }
    }
}

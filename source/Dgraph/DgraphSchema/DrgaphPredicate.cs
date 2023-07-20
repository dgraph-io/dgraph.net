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

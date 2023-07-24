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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dgraph.tests.e2e.Tests.TestClasses
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Person
    {
        public string Uid { get; set; }
        [JsonProperty("dgraph.type")]
        public string Type { get; } = "Person";
        public string Name { get; set; }
        public List<Person> Friends { get; } = new List<Person>();
        public DateTime Dob { get; set; }
        public double Height { get; set; }
        public List<int> Scores { get; } = new List<int>();
    }
}

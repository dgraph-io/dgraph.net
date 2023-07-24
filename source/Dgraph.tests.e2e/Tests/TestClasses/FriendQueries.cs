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

using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Dgraph.tests.e2e.Tests.TestClasses
{
    public class FriendQueries
    {
        public static string QueryByUid(string uid) =>
            "{  "
            + $"    q(func: uid({uid})) "
            + "     {   "
            + "        uid  "
            + "        name  "
            + "        dob  "
            + "        height  "
            + "        scores  "
            + "        friends {   "
            + "            uid  "
            + "            name  "
            + "            dob  "
            + "            height  "
            + "            scores   "
            + "        }   "
            + "    }   "
            + "}";

        public static string QueryByName = @"
query people($name: string) {
    q(func: eq(name, $name)) {
        uid
        name
        dob
        height
        scores
        friends {
            uid
            name
            dob
            height
            scores
        }
    }
}";

        public static void AssertStringIsPerson(string json, Person person)
        {
            var people = JObject.Parse(json)["q"].ToObject<List<Person>>();
            people.Count.Should().Be(1);
            people[0].Should().BeEquivalentTo(person);
        }

    }
}

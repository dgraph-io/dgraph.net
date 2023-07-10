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

namespace Dgraph.tests.e2e.Orchestration
{
    public class TestExecutor
    {
        public int TestsRun = 0;
        public int TestsFailed = 0;
        public IReadOnlyList<Exception> Exceptions => _Exceptions;
        private List<Exception> _Exceptions = new List<Exception>();

        private readonly TestFinder TestFinder;
        private readonly DgraphClientFactory ClientFactory;

        public TestExecutor(TestFinder testFinder, DgraphClientFactory clientFactory)
        {
            TestFinder = testFinder;
            ClientFactory = clientFactory;
        }

        public async Task ExecuteAll(IEnumerable<string> tests)
        {
            foreach (var test in TestFinder.FindTests(tests))
            {
                try
                {
                    TestsRun++;
                    await test.Setup();
                    await test.Test();
                    await test.TearDown();
                }
                catch (Exception ex)
                {
                    TestsFailed++;
                    _Exceptions.Add(ex);
                }
            }
        }

    }
}

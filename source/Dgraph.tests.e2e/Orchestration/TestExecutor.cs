using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
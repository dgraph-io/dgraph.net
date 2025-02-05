/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

using Dgraph.tests.e2e.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Dgraph.tests.e2e.Orchestration
{
    public class TestFinder
    {
        private readonly IServiceProvider ServiceProvider;

        public TestFinder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IReadOnlyList<string> FindTestNames(IEnumerable<string> prefixes = null)
        {

            Type baseTestType = typeof(DgraphDotNetE2ETest);
            var allTestNames = typeof(DgraphDotNetE2ETest).Assembly.GetTypes().Where(t => t.IsSubclassOf(baseTestType)).Select(t => t.Name);

            var tests = prefixes == null || !prefixes.Any()
                ? allTestNames
                : allTestNames.Where(tn => prefixes.Any(t => tn.StartsWith(t)));

            return tests.ToList();
        }

        public IReadOnlyList<DgraphDotNetE2ETest> FindTests(IEnumerable<string> testNames) =>
            testNames.Select(tn => FindTestByName(tn)).ToList();

        // This isn't perfect.  I can't see another way though without using
        // something like Autofac.  As is, any new test needs to be registered
        // in here.  There's no way to just mint up the instances from the
        // names, so without this there's no way to just run a particular test.
        public DgraphDotNetE2ETest FindTestByName(string name)
        {
            switch (name)
            {
                case "SchemaTest":
                    return ServiceProvider.GetService<SchemaTest>();
                case "MutateQueryTest":
                    return ServiceProvider.GetService<MutateQueryTest>();
                case "TransactionTest":
                    return ServiceProvider.GetService<TransactionTest>();
                // case "UpsertTest":
                //     return ServiceProvider.GetService<UpsertTest>();
                default:
                    throw new KeyNotFoundException($"Couldn't find test : {name}.  Ensure all tests are registered in {nameof(TestFinder)}");
            }
        }
    }
}

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

using Dgraph.tests.e2e.Errors;
using Dgraph.tests.e2e.Orchestration;
using Dgraph.tests.e2e.Tests;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Dgraph.tests.e2e
{
    [Command(Name = "Dgraph.net E2E test runner")]
    [HelpOption("--help")]
    class Program
    {

        [Option(ShortName = "t", Description = "Set the tests to actually run.  Can be set multiple times.  Not setting == run all tests.")]
        public List<string> Test { get; } = new List<string>();

        [Option(ShortName = "i", Description = "Turn on interactive mode when not running in build server.")]
        public bool Interactive { get; }

        public static int Main(string[] args)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddEnvironmentVariables("DGNETE2E_")
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                var services = new ServiceCollection();

                // Inject in every possible test type so that DI will be able to
                // mint up these for me without me having to do anything to hydrate
                // the objects.
                Type baseTestType = typeof(DgraphDotNetE2ETest);
                var assembly = typeof(DgraphDotNetE2ETest).Assembly;
                IEnumerable<Type> testTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(baseTestType));
                foreach (var testType in testTypes)
                {
                    services.AddTransient(testType);
                }

                services.AddSingleton<TestFinder>();
                services.AddTransient<TestExecutor>();
                services.AddScoped<DgraphClientFactory>();

                var serviceProvider = services.BuildServiceProvider();

                var app = new CommandLineApplication<Program>();
                app.Conventions
                    .UseDefaultConventions()
                    .UseConstructorInjection(serviceProvider);

                app.Execute(args);
                return 0;

            }
            catch (AggregateException aggEx)
            {
                foreach (var ex in aggEx.InnerExceptions)
                {
                    switch (ex)
                    {
                        case DgraphDotNetTestFailure testEx:
                            Log.Error("Test Failed with reason {@Reason}", testEx.FailureReason);
                            Log.Error(testEx, "Call Stack");
                            break;
                        default:
                            Log.Error(ex, "Unknown Exception Failure");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Test run failed.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            return 1;
        }

        public Program(IServiceProvider serviceProvider, TestFinder testFinder, TestExecutor testExecutor)
        {
            ServiceProvider = serviceProvider;
            TestFinder = testFinder;
            TestExecutor = testExecutor;
        }

        private IServiceProvider ServiceProvider;
        private TestFinder TestFinder;
        private TestExecutor TestExecutor;

        private async Task OnExecuteAsync(CommandLineApplication app)
        {

            EnsureAllTestsRegistered();

            var tests = TestFinder.FindTestNames(Test);

            Log.Information("Begining {NumTests} tests.", tests.Count);

            // Exceptions shouldn't escape this in normal circumstances.
            var executor = await Execute(tests);

            var totalRan = executor.TestsRun;
            var totalFailed = executor.TestsFailed;
            var exceptionList = executor.Exceptions;

            Log.Information("-----------------------------------------");
            Log.Information("Test Results:");
            Log.Information($"Tests Run: {totalRan}");
            Log.Information($"Tests Succesful: {totalRan - totalFailed}");
            Log.Information($"Tests Failed: {totalFailed}");
            Log.Information("-----------------------------------------");

            if (totalFailed > 0)
            {
                throw new AggregateException(exceptionList);
            }
        }

        private async Task<TestExecutor> Execute(IEnumerable<string> tests)
        {
            using (ServiceProvider.CreateScope())
            {
                TestExecutor exec = ServiceProvider.GetService<TestExecutor>();
                await exec.ExecuteAll(tests);
                return exec;
            }
        }

        private void EnsureAllTestsRegistered() =>
            TestFinder.FindTests(TestFinder.FindTestNames());
    }
}

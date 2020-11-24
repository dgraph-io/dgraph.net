using System;
using System.Threading.Tasks;
using Dgraph.tests.e2e.Orchestration;
using System.Threading;

namespace Dgraph.tests.e2e.Tests
{

    public class JWTTest : MutateQueryTest {

        private readonly DgraphTestSettings _settings;

        public JWTTest(
            DgraphClientFactory clientFactory,
            ACLInitializer setup,
            DgraphTestSettings settings) : base(clientFactory, setup) { 
                _settings = settings;
            }

        public async override Task Test() {

            // Same as the tests it inherits from MutateQueryTest
            // we just sleep long enough between each test for the
            // JWT to expire, and thus

            using(var client = await ClientFactory.GetDgraphClient()) {
                await AddThreePeople(client);

                Thread.Sleep(
                    TimeSpan.FromSeconds(_settings.JWTSleep == 0 ? 10 : _settings.JWTSleep));

                await QueryAllThreePeople(client);

                Thread.Sleep(
                    TimeSpan.FromSeconds(_settings.JWTSleep == 0 ? 10 : _settings.JWTSleep));

                await AlterAPerson(client);

                Thread.Sleep(
                    TimeSpan.FromSeconds(_settings.JWTSleep == 0 ? 10 : _settings.JWTSleep));

                await QueryWithVars(client);

                Thread.Sleep(
                    TimeSpan.FromSeconds(_settings.JWTSleep == 0 ? 10 : _settings.JWTSleep));

                await DeleteAPerson(client);
            }
        }

    }
}
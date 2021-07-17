using System.Threading.Tasks;
using Assent;
using Dgraph.tests.e2e.Orchestration;
using FluentAssertions;
using Newtonsoft.Json;
using Dgraph.Schema;
using Api;
using System;
using System.Threading;

namespace Dgraph.tests.e2e.Tests
{
    public class SchemaTest : DgraphDotNetE2ETest {
        public SchemaTest(IDgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Test() {
            using(var client = await ClientFactory.GetDgraphClient()) {
                await InitialSchemaIsAsExpected(client);
                await AlterSchemAsExpected(client);
                await AlterSchemaAgainAsExpected(client);
                await SchemaQueryWithRestrictions(client);

                await ErrorsResultInFailedQuery(client);
            }
        }

        private async Task InitialSchemaIsAsExpected(IDgraphClient client) {
            var response = await client.NewReadOnlyTransaction().Query("schema {}");
            AssertResultIsSuccess(response);

            var schema = JsonConvert.DeserializeObject<DgraphSchema>(response.Value.Json);
            this.Assent(schema.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemAsExpected(IDgraphClient client) {
            var alterSchemaResult = await client.Alter(
                new Operation{ Schema = ReadEmbeddedFile("test.schema") });
            AssertResultIsSuccess(alterSchemaResult);

            // After an Alter, Dgraph computes indexes in the background.
            // So a first schema query after Alter might return a schema
            // without indexes.  We could poll and backoff and show that
            // ... but we aren't testing that here, just that the schema
            // updates.
            Thread.Sleep(TimeSpan.FromSeconds(5));

            var response = await client.NewReadOnlyTransaction().Query("schema {}");
            AssertResultIsSuccess(response);

            var schema = JsonConvert.DeserializeObject<DgraphSchema>(response.Value.Json);
            this.Assent(schema.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemaAgainAsExpected(IDgraphClient client) {
            var alterSchemaResult = await client.Alter(
                new Operation{ Schema = ReadEmbeddedFile("altered.schema") });
            AssertResultIsSuccess(alterSchemaResult);

            Thread.Sleep(TimeSpan.FromSeconds(5));

            var response = await client.NewReadOnlyTransaction().Query("schema {}");
            AssertResultIsSuccess(response);

            var schema = JsonConvert.DeserializeObject<DgraphSchema>(response.Value.Json);
            this.Assent(schema.ToString(), AssentConfiguration);
        }

        private async Task SchemaQueryWithRestrictions(IDgraphClient client) {
            var response = await client.NewReadOnlyTransaction().Query(
                "schema(pred: [name, friends, dob, scores]) { type }");
            AssertResultIsSuccess(response);

            var schema = JsonConvert.DeserializeObject<DgraphSchema>(response.Value.Json);
            this.Assent(schema.ToString(), AssentConfiguration);
        }

        private async Task ErrorsResultInFailedQuery(IDgraphClient client) {
            // maformed
            var q1result = await client.NewReadOnlyTransaction().Query(
                "schema(pred: [name, friends, dob, scores]) { type ");
            q1result.IsSuccess.Should().BeFalse();
        }
    }
}
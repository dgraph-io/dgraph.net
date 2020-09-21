using System.Threading.Tasks;
using Assent;
using Dgraph.tests.e2e.Orchestration;
using FluentAssertions;
using Newtonsoft.Json;
using Dgraph.Schema;
using Api;

namespace Dgraph.tests.e2e.Tests
{
    public class SchemaTest : DgraphDotNetE2ETest {
        public SchemaTest(
            DgraphClientFactory clientFactory,
            ACLInitializer setup) : base(clientFactory, setup) { }

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

            var response = await client.NewReadOnlyTransaction().Query("schema {}");
            AssertResultIsSuccess(response);

            var schema = JsonConvert.DeserializeObject<DgraphSchema>(response.Value.Json);
            this.Assent(schema.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemaAgainAsExpected(IDgraphClient client) {
            var alterSchemaResult = await client.Alter(
                new Operation{ Schema = ReadEmbeddedFile("altered.schema") });
            AssertResultIsSuccess(alterSchemaResult);

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
            // malformed
            var q1result = await client.NewReadOnlyTransaction().Query(
                "schema(pred: [name, friends, dob, scores]) { type ");
            q1result.IsSuccess.Should().BeFalse();
        }
    }
}
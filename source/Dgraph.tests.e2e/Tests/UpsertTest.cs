using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dgraph.tests.e2e.Orchestration;
using Dgraph.tests.e2e.Tests.TestClasses;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Api;
using Dgraph.Transactions;

namespace Dgraph.tests.e2e.Tests
{
    public class UpsertTest : MutateQueryTest {

        public UpsertTest(
            DgraphClientFactory clientFactory,
            ACLInitializer setup) : base(clientFactory, setup) { }

        public async override Task Setup() {
            await base.Setup();
        }

        public async override Task Test() {
            using(var client = await ClientFactory.GetDgraphClient()) {
                await UpsertNewNodes(client);
                await UpsertWithExisting(client);
            }
        }

        private async Task UpsertNewNodes(IDgraphClient client) {
            using(var transaction = client.NewTransaction()) {

                Person1.Uid = "uid(person1)";
                Person2.Uid = "uid(person2)";
                var p1json = JsonConvert.SerializeObject(Person1);
                var p2json = JsonConvert.SerializeObject(Person2);

                var request = new RequestBuilder {
                    // Set the query that will be used in the conditional mutations.
                    // In this case, we'll only add if the people don't exist.
                    Query = @"{ 
                        getP1(func: eq(name, ""Person1"")) { person1 as uid } 
                        getP2(func: eq(name, ""Person2"")) { person2 as uid } 
                    }",
                }.WithMutations(
                    new MutationBuilder {
                        SetJson = p1json,
                        Cond = "@if(eq(len(person1), 0))"
                    },
                    new MutationBuilder {
                        SetJson = p2json,
                        Cond = "@if(eq(len(person2), 0))"
                    }
                );

                var upsertResult = await transaction.Mutate(request);
                AssertResultIsSuccess(upsertResult);

                // Neither Person1 or Person2 are in the DB, so both should have
                // been added
                upsertResult.Value.Uids.Count.Should().Be(2);
                upsertResult.Value.Uids["uid(person1)"].Should().NotBeNull();
                upsertResult.Value.Uids["uid(person1)"].Should().NotBeNull();
                upsertResult.Value.Json.Should().Be("{\"getP1\":[],\"getP2\":[]}");

                var transactionResult = await transaction.Commit();
                AssertResultIsSuccess(transactionResult);

            }        
        }


        private async Task UpsertWithExisting(IDgraphClient client) {
            using(var transaction = client.NewTransaction()) {

                Person3.Uid = "uid(person3)";
                var p2json = JsonConvert.SerializeObject(Person2);
                var p3json = JsonConvert.SerializeObject(Person3);

                var request = new RequestBuilder {
                    // Set the query that will be used in the conditional mutations.
                    // In this case, we'll only add Person3, because the others already
                    // exist.
                    Query = @"{ 
                        getP1(func: eq(name, ""Person1"")) { person1 as uid } 
                        getP2(func: eq(name, ""Person2"")) { person2 as uid } 
                        getP3(func: eq(name, ""Person3"")) { person3 as uid } 
                    }",
                }.WithMutations(
                    new MutationBuilder {
                        SetJson = p2json,
                        Cond = "@if(eq(len(person2), 0))"
                    },
                    new MutationBuilder {
                        SetJson = p3json,
                        Cond = "@if(eq(len(person3), 0))"
                    }
                );

                var upsertResult = await transaction.Mutate(request);
                AssertResultIsSuccess(upsertResult);

                // Person2 is in the DB, so that conditional mutation doesn't get run.
                // For Person3, it links via friend to Person1, but we use the existing
                // UID from the query, so this links to the existing node, and doesn't
                // recreate.
                upsertResult.Value.Uids.Count.Should().Be(1);
                upsertResult.Value.Uids["uid(person3)"].Should().NotBeNull();

                JObject.Parse(upsertResult.Value.Json)["getP1"].
                    ToObject<List<Person>>().Should().HaveCount(1);
                JObject.Parse(upsertResult.Value.Json)["getP2"].
                    ToObject<List<Person>>().Should().HaveCount(1);
                JObject.Parse(upsertResult.Value.Json)["getP3"].
                    ToObject<List<Person>>().Should().HaveCount(0);

                var transactionResult = await transaction.Commit();
                AssertResultIsSuccess(transactionResult);
            }        
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dgraph.tests.e2e.Orchestration;
using Dgraph.tests.e2e.Tests.TestClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Api;
using Dgraph.Transactions;

// This is a simple example of some of the features of the 
// Dgraph C# client.  The other tests make sure the features
// are working.  This one more shows how you'd use it in an
// app.

namespace Dgraph.tests.e2e.Tests
{
    public class DgraphDotNetExample : DgraphDotNetE2ETest {

        private Person Person1, Person2, Person3;

        public DgraphDotNetExample(
            DgraphClientFactory clientFactory,
            ACLInitializer setup) : base(clientFactory, setup) { }

        public async override Task Setup() {
            await base.Setup();

            Person1 = new Person() {
                Uid = "_:Person1",
                Name = "Person1",
                Dob = new DateTime(1991, 1, 1),
                Height = 1.78,
                Scores = { 1, 32, 45, 62 }
            };

            Person2 = new Person() {
                Uid = "_:Person2",
                Name = "Person2",
                Dob = new DateTime(1992, 1, 1),
                Height = 1.85,
                Scores = { 3, 2, 1 }
            };

            Person3 = new Person() {
                Uid = "_:Person3",
                Name = "Person3",
                Dob = new DateTime(1993, 1, 1),
                Height = 1.85,
                Scores = { 32, 21, 10 },
            };
            Person3.Friends.Add(Person1);
        }

        public async override Task Test() {

            // Create a client.  Generally, you'd connect the the gRPC ports
            // on the alphas you want to connect to with
            //
            // GrpcChannel.ForAddress("http:...:9080")
            //
            // and then intantiate the client with
            //
            // new DgraphClient(chan1, chan2, ...)
            //
            // The Dgraph client takes control of the channels and disposes them
            // when the client is disposed.  So you don't need to dispose channels,
            // but you should dispose of the client when done.  Normally, you'd wrap
            // it in a using, like:
            //
            // using(var client = new DgraphClient(chan1, chan2, ...) { ... }

            using(var client = await ClientFactory.GetDgraphClient()) {

                // Alter the database to have the right schema like this.
                var alterSchemaResult = await client.Alter(
                    new Operation{ Schema = ReadEmbeddedFile("test.schema") });
                if(alterSchemaResult.IsFailed) {
                    // Do whatever is sensible on failure for your app...
                    return;
                }

                // transactions
                //
                // Making changes needs a transaction.
                // Wrap that in a using, so that the transaction gets discarded if
                // you leave without committing.
                using(var transaction = client.NewTransaction()) {

                    // Serialize the objects to json in whatever way works best for you.
                    var personList = new List<Person> { Person1, Person2, Person3 };
                    var json = JsonConvert.SerializeObject(personList);

                    // There's two mutation options.  For just one mutation, use this version
                    var result = await transaction.Mutate(setJson: json);
                    if(result.IsFailed) {
                        // Do whatever is sensible on failure for your app...
                        //
                        // You can discard a transaction with
                        //
                        // await transaction.Discard();
                        //
                        // But because the transaction is inside a using,
                        // that'll get done automatically.
                        return;
                    }

                    // The payload of the result contains a node->uid map of newly
                    // allocated nodes.  If the nodes don't have uid names in the
                    // mutation, then the map is like
                    //
                    // {{ "blank-0": "0xa", "blank-1": "0xb", "blank-2": "0xc", ... }}
                    //
                    // If the
                    // mutation has '{ "uid": "_:Person1" ... }' etc, then the blank
                    // node map is like
                    // 
                    // {{ "Person3": "0xe", "Person1": "0xf", "Person2": "0xd", ... }}
                    //
                    // That's why the data setup the objects like
                    //
                    //  Person1 = new Person() { Uid = "_:Person1", ... }
                    //
                    // So we could now grab the actual unique IDs like
                    //
                    // Person1.Uid = result.Value.Uids[Person1.Uid.Substring(2)];


                    // Commit the transaction on success.  
                    var transactionResult = await transaction.Commit();
                    if(transactionResult.IsFailed) {
                        // If the transaction failed to commit.  then it's had
                        // no effect and can't be reused.  
                        return;
                    }
                }

                // Upsert transactions
                //
                // Sometimes more complicated mutations need multiple mutations in
                // one request and upserts like this.
                using(var transaction = client.NewTransaction()) {

                    var P2json = JsonConvert.SerializeObject(new Person() {
                        Uid = "uid(person2)",
                        Name = "Person2",
                        Dob = new DateTime(1992, 1, 1),
                        Height = 1.85,
                        Scores = { 3, 2, 1 }
                    });

                    var request = new RequestBuilder {
                        // Set the query that will be used in the conditional mutations.
                        // In this case, we only add Person2, if it doesn't already exist. 
                        Query = "{ getP2(func: eq(name, \"Person2\")) { person2 as uid } }",

                        // If set, the transaction will commit as part of processing
                        // the request
                        CommitNow = true
                    }.WithMutations(
                        new MutationBuilder {
                            SetJson = P2json,
                            Cond = "@if(eq(len(person2), 0))"
                        }
                        // you can have multiple mutations which only get
                        // run if the `Cond` evaluates to true
                    );

                    var result = await transaction.Mutate(request);
                    if(result.IsFailed) {
                        // Do whatever is sensible on failure for your app...
                        return;
                    }

                    // result.Value.Json will contain the results of the query
                    // that ran as part of the mutation.  And if new nodes
                    // were added, then those nodes will be in result.Value.Uids

                    // No need to commit because we set `CommitNow = true`
                }

                // You can run queries inside transactions, but if you don't
                // need to make a mutation, then a readonly query can be more efficient
                var queryResult = await client.NewReadOnlyTransaction().QueryWithVars(
                    @"
                        query people($name: string) {
                            person(func: eq(name, $name)) {
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
                        }",
                    new Dictionary<string, string> { { "$name", Person3.Name } });

                if(queryResult.IsFailed) {
                    // Do whatever is sensible on failure for your app...
                    return;
                }

                // Deserialise the result however works best for you.  The query 
                // result is json like 
                //
                // { "person": [ { "name": ... }, ... ] }
                //
                // So you'll need to either have an object that includes the
                // top level query name, or dig in programatically.
                var p = JObject.Parse(queryResult.Value.Json)["person"].ToObject<List<Person>>();
            }   
        }
    }
}
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Dgraph.tests.e2e.Orchestration
{

    public class GraphQLRequest {
        public string Query { get; set; }
    }

    public class ACLInitializer {

        private readonly DgraphTestSettings _settings;

        public ACLInitializer(DgraphTestSettings settings) {
            _settings = settings;
        }

        public bool ACL_Enabled() => _settings.GrootPassword != null;

        public async Task DgraphSetup() {

            if(!ACL_Enabled()) {
                return;
            }

            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri(_settings.AlphaHTTP);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var loginReq = new GraphQLRequest {
                    Query = @"mutation {
                                login(
                                    userId: ""groot"", 
                                    password: """ + _settings.GrootPassword + @"""
                                ) {
                                    response {
                                        accessJWT
                                        refreshJWT
                                    }
                                }
                            }"
                };

            HttpResponseMessage loginResponse = await client.PostAsJsonAsync("admin", loginReq);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            dynamic login = JObject.Parse(loginResult);
            string jwt = login.data.response.accessJWT;
            client.DefaultRequestHeaders.Add("X-Dgraph-AccessToken", jwt);

            HttpResponseMessage userResponse = await client.PostAsJsonAsync(
                "admin", 
                new GraphQLRequest {
                    Query = "mutation { addUser(input: [ {" +
                        $"name: \"{_settings.TestUser}\", password: \"{_settings.TestUserPassword}\"," + 
                        "groups: [ { name: \"dev\" } ]" +
                    "}]) { user { name } } }"
                });
            userResponse.EnsureSuccessStatusCode();

            var updGroup = new GraphQLRequest {
                    Query = @"mutation {
                        updateGroup(input: {
                            filter: { name: { eq: ""dev""} }, 
                            set: { rules: [ 
                                { predicate: ""abool"", permission: 7 },
                                { predicate: ""car"", permission: 7 },
                                { predicate: ""carMake"", permission: 7 },
                                { predicate: ""dob"", permission: 7 },
                                { predicate: ""height"", permission: 7 },
                                { predicate: ""name"", permission: 7 },
                                { predicate: ""friends"", permission: 7 },
                                { predicate: ""scores"", permission: 7 },
                                { predicate: ""_STAR_ALL"", permission: 3 },
                                { predicate: ""dgraph.type"", permission: 3 }
                            ] }
                        } ) {
                            group { name }
                        }
                    }"
                };

            HttpResponseMessage groupResponse = await client.PostAsJsonAsync("admin", updGroup);
            groupResponse.EnsureSuccessStatusCode();

            // You should set --acl_cache_ttl 5s so the cache resets before the tests run
            Thread.Sleep(TimeSpan.FromSeconds(_settings.ACLSleep == 0 ? 10 : _settings.ACLSleep));
        }

    }
}

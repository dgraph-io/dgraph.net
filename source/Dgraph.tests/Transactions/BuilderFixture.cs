using Dgraph.Transactions;
using FluentAssertions;
using Google.Protobuf;
using NUnit.Framework;

namespace Dgraph.tests.Transactions
{
    public class BuilderFixture {

        [TestCase(null, null, null)]
        [TestCase("query", null, null)]
        [TestCase(null, true, null)]
        [TestCase(null, null, true)]
        [TestCase("query", true, true)]
        public void MutationRequestsAreNeverReadonly(string query, bool? commit, bool withMutation) {
            var builder = new RequestBuilder();
            
            if(query != null) {
                builder.Query = query;
            }
            if(commit != null) {
                builder.CommitNow = commit.Value;
            }
            if(withMutation) {
                builder.WithMutations(new MutationBuilder());
            }

            var req = builder.Request;

            req.BestEffort.Should().BeFalse();
            req.ReadOnly.Should().BeFalse();
        }

        [TestCase(null, null, null)]
        [TestCase("query", null, null)]
        [TestCase(null, true, null)]
        [TestCase(null, null, true)]
        [TestCase("query", true, true)]
        public void MutationRequestsPreservesArgs(string query, bool? commit, bool withMutation) {
            var builder = new RequestBuilder();
            
            if(query != null) {
                builder.Query = query;
            }
            if(commit != null) {
                builder.CommitNow = commit.Value;
            }

            var mb = new MutationBuilder() { SetJson = "json"};
            if(withMutation) {
                builder.WithMutations(mb);
            }

            var req = builder.Request;

            req.Query.Should().Be(query ?? "");
            req.CommitNow.Should().Be(commit ?? false);
        
            if(withMutation) {
                req.Mutations.Count.Should().Be(1);
                req.Mutations[0].Should().Be(mb.Mutation);
            } else {
                req.Mutations.Count.Should().Be(0);
            }
        }

        [TestCase(null, null, null, null, null)]
        [TestCase("set json", null, null, null, null)]
        [TestCase(null, "set nq", null, null, null)]
        [TestCase(null, null, "del json", null, null)]
        [TestCase(null, null, null, "del nq", null)]
        [TestCase(null, null, null, null, "cond")]
        [TestCase("set json", "set nq", "del json", "del nq", "cond")]
        public void MutationBuilderPreservesArgs(
            string setJson, 
            string setNQ,
            string deleteJson,
            string deleteNQ,
            string cond
        ) {

            var mb = new MutationBuilder {
                SetJson = setJson,
                SetNquads = setNQ,
                DeleteJson = deleteJson,
                DelNquads = deleteNQ,
                Cond = cond                
            };

            mb.Mutation.Should().Be(
                new Api.Mutation {
                    SetJson = ByteString.CopyFromUtf8(setJson ?? ""),
                    SetNquads = ByteString.CopyFromUtf8(setNQ ?? ""),
                    DeleteJson = ByteString.CopyFromUtf8(deleteJson ?? ""),
                    DelNquads = ByteString.CopyFromUtf8(deleteNQ ?? ""),
                    Cond = cond ?? ""
                }
            );
        }
    }
}
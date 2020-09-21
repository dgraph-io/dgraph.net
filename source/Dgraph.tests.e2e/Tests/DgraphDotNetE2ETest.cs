using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Assent.Namers;
using Dgraph.tests.e2e.Errors;
using Dgraph.tests.e2e.Orchestration;
using FluentResults;
using Microsoft.Extensions.FileProviders;

namespace Dgraph.tests.e2e.Tests
{
    public abstract class DgraphDotNetE2ETest {
        protected readonly DgraphClientFactory ClientFactory;
        protected readonly ACLInitializer _setup;

        protected readonly Assent.Configuration AssentConfiguration;

        private readonly IFileProvider EmbeddedProvider;

        public DgraphDotNetE2ETest(
            DgraphClientFactory clientFactory,
            ACLInitializer setup) {
            ClientFactory = clientFactory;
            _setup = setup;

            if(_setup.ACL_Enabled()) {
                AssentConfiguration = new Assent.Configuration()
                    .UsingNamer(new SubdirectoryNamer("Approved/ACL"));
            } else {
                AssentConfiguration = new Assent.Configuration()
                .UsingNamer(new SubdirectoryNamer("Approved"));
            }

            EmbeddedProvider = new EmbeddedFileProvider(
                Assembly.GetAssembly(typeof(DgraphDotNetE2ETest)), 
                "Dgraph.tests.e2e.Tests.Data");
        }

        public async virtual Task Setup() {
            using(var client = await ClientFactory.GetDgraphClient(groot: true)) {
                var result = await client.Alter(new Api.Operation { DropAll = true }); 
                if (result.IsFailed) {
                    throw new DgraphDotNetTestFailure("Failed to clean database in test setup", result);
                }
            }
            await _setup.DgraphSetup();
        }

        public abstract Task Test();

        public async virtual Task TearDown() {
            using(var client = await ClientFactory.GetDgraphClient(groot: true)) {
                var result = await client.Alter(
                    new Api.Operation { DropAll = true }); 
                if (result.IsFailed) {
                    throw new DgraphDotNetTestFailure("Failed to clean database in test setup", result);
                }
            }
        }

        protected string ReadEmbeddedFile(string filename) {
            using(var stream = EmbeddedProvider.GetFileInfo(filename).CreateReadStream()) {
                using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                    return reader.ReadToEnd();
                }
            }
        }

        protected void AssertResultIsSuccess(ResultBase result, string msg = null) {
            if(result.IsFailed) {
                throw new DgraphDotNetTestFailure(msg ?? "Expected success result, but got failed", result);
            }
        }

    }
}
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

using System.Reflection;
using System.Text;
using Assent.Namers;
using Dgraph.tests.e2e.Errors;
using Dgraph.tests.e2e.Orchestration;
using FluentResults;
using Microsoft.Extensions.FileProviders;
using Serilog;

namespace Dgraph.tests.e2e.Tests
{
    public abstract class DgraphDotNetE2ETest
    {
        protected readonly DgraphClientFactory ClientFactory;

        protected readonly Assent.Configuration AssentConfiguration;

        private readonly IFileProvider EmbeddedProvider;

        public DgraphDotNetE2ETest(DgraphClientFactory clientFactory)
        {
            ClientFactory = clientFactory;

            AssentConfiguration = new Assent.Configuration()
                .UsingNamer(new SubdirectoryNamer("Approved"))
                .UsingReporter((received, approved) =>
                {
                    Log.Warning("Expected {received}, got {approved}", received, approved);
                });
            // FIXME: .UsingSanitiser(...) might want to add this to remove versions etc
            // FIXME: when I add this to a build pipeline it needs this turned off when running on the build server
            // .SetInteractive(...);

            EmbeddedProvider = new EmbeddedFileProvider(Assembly.GetAssembly(typeof(DgraphDotNetE2ETest)), "Dgraph.tests.e2e.Tests.Data");
        }

        public async virtual Task Setup()
        {
            using (var client = await ClientFactory.GetDgraphClient())
            {
                var result = await client.Alter(
                    new Api.Operation { DropAll = true });
                if (result.IsFailed)
                {
                    throw new DgraphDotNetTestFailure("Failed to clean database in test setup", result);
                }
            }
        }

        public abstract Task Test();

        public async virtual Task TearDown()
        {
            using (var client = await ClientFactory.GetDgraphClient())
            {
                var result = await client.Alter(
                    new Api.Operation { DropAll = true });
                if (result.IsFailed)
                {
                    throw new DgraphDotNetTestFailure("Failed to clean database in test setup", result);
                }
            }
        }

        protected string ReadEmbeddedFile(string filename)
        {
            using (var stream = EmbeddedProvider.GetFileInfo(filename).CreateReadStream())
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        protected void AssertResultIsSuccess(ResultBase result, string msg = null)
        {
            if (result.IsFailed)
            {
                throw new DgraphDotNetTestFailure(msg ?? "Expected success result, but got failed", result);
            }
        }

    }
}

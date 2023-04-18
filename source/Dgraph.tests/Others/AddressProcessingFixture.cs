using System;
using System.Collections.Generic;
using NUnit.Framework;
using Dgraph;

namespace Dgraph.tests.Others
{
    public class AddressProcessingFixture
    {
        [Test]
        public void TestProcessAddress()
        {
            var testData = new Dictionary<string, string>
            {
                {"https://invalid-example.com/graphql", "https://example.grpc.us-east-1.aws.cloud.dgraph.io"},
                {"http://example.us-east-1.aws.cloud.dgraph.io/graphql", "https://example.grpc.us-east-1.aws.cloud.dgraph.io"},
                {"example.us-east-1.aws.cloud.dgraph.io/graphql", "https://example.grpc.us-east-1.aws.cloud.dgraph.io"},
                {"example.grpc.us-east-1.aws.cloud.dgraph.io", "https://example.grpc.us-east-1.aws.cloud.dgraph.io"},
                {"https://example.us-west-1.aws.cloud.dgraph.io", "https://example.grpc.us-west-1.aws.cloud.dgraph.io"},
                {"http://example.grpc.us-west-1.aws.cloud.dgraph.io", "https://example.grpc.us-west-1.aws.cloud.dgraph.io"},
                {"example.us-west-1.aws.cloud.dgraph.io", "https://example.grpc.us-west-1.aws.cloud.dgraph.io"},
            };

            foreach (var testPair in testData)
            {
                var processedAddress = DgraphCloudChannel.ProcessAddressForTest(testPair.Key);
                Assert.AreEqual(testPair.Value, processedAddress);
            }
        }

        [TestCase("https://invalid-example.com")]
        [TestCase("http://invalid-example.io")]
        [TestCase("invalid-example.io")]
        [TestCase("example.invalid-domain.io")]
        [TestCase("https://example.invalid-domain.io")]
        [TestCase("http://example.invalid-domain.io")]
        [TestCase("example.us-east-1.aws.invalid-domain.io")]
        [TestCase("https://example.us-west-1.aws.invalid-domain.io/graphql")]
        [TestCase("http://example.grpc.us-west-1.aws.invalid-domain.io/graphql")]
        public void TestProcessAddress_ShouldFail(string invalidAddress)
        {
            Assert.Throws<ArgumentException>(() => DgraphCloudChannel.ProcessAddressForTest(invalidAddress));
        }
    }
}

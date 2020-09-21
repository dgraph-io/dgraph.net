using System.Collections.Generic;

namespace Dgraph.tests.e2e {

    // Depending on the settings, we make different kinds of connections.
    // 
    // There's the option of having tls or not, and of running in 
    // enterprise mode or not ... so you could be needing a username
    // password, but no tls, just tls, or both or none :-)
    //
    // To test all the cases you want, you'd need to externally configure
    // Dgraph into the security state you want and then run the tests with
    // the appropriate appsetings.json, or environment variables.

    public class DgraphTestSettings {

        // Endpoints to connect to
        public DgraphTestEndpoint[] Endpoints { get; set; }

        // ACL user tests
        public string AlphaHTTP { get; set; }
        public string GrootPassword { get; set; }
        public string TestUser { get; set; }
        public string TestUserPassword { get; set; }

        public int JWTSleep { get; set; }
        public int ACLSleep { get; set; }

        // TLS settings
        public string CaCert { get; set; }
        public string ClientCert { get; set; }
        public string ClientKey { get; set; }
    }

    public class DgraphTestEndpoint {
        public string EndPoint { get; set; }
    }

}

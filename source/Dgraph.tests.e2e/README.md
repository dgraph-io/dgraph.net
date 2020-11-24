# E2E Tests

Run with just

```
dotnet run
```

There's a bit of setup.  See `appsettings.json`.  The `DgraphTestSettings` control the tests.

There's a bunch of ways the tests can be run.

* no auth 
* tls 
* acl
* tls + acl

e.g.:

```
    "DgraphTestSettings": {
      "Endpoints": [
        { "EndPoint": "http://127.0.0.1:9080" }
      ],
      "AlphaHTTP": "http://127.0.0.1:8080/",
      "GrootPassword": "password",
      "TestUser": "myuser",
      "TestUserPassword": "pwd-02"
    }  
```

You can also set these with environment variables so you can run the tests in different scenarios in an automated way.

You'll need to set the `--acl_cache_ttl` to a value less than `ACLSleep`: e.g. `--acl_cache_ttl 5s` and `ACLSleep: 10` (seconds).  This makes the tests wait for the acl cache to reset after new permissions have been applied.

Same for `--acl_access_ttl` and JWTSleep, which makes the `JWTTest` wait long enough to force using the JWT refresh token.
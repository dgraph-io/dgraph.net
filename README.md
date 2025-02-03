# Dgraph.net [![Nuget](https://img.shields.io/nuget/v/dgraph)](https://www.nuget.org/packages/Dgraph)

This client follows the [Dgraph Go client][goclient] closely.

[goclient]: https://github.com/dgraph-io/dgo

Before using this client, we highly recommend that you go through [dgraph.io/docs], and understand
how to run and work with Dgraph.

**Use [Discuss Issues](https://discuss.dgraph.io/tags/c/issues/35/dgraphnet) for reporting issues
about this repository.**

[dgraph.io/docs]: https://dgraph.io/docs

## Table of contents

- [Install](#install)
- [Supported Versions](#supported-versions)
- [Using a client](#using-a-client)
  - [Creating a client](#creating-a-client)
  - [Login into a namespace](#login-into-a-namespace)
  - [Connecting To Dgraph Cloud](#connecting-to-dgraph-cloud)
  - [Altering the database](#altering-the-database)
  - [Creating a transaction](#creating-a-transaction)
  - [Running a mutation](#running-a-mutation)
  - [Running a query](#running-a-query)
  - [Query with RDF response](#query-with-rdf-response)
  - [Running an Upsert: Query + Mutation](#running-an-upsert-query--mutation)
  - [Running Conditional Upsert](#running-conditional-upsert)
  - [Committing a transaction](#committing-a-transaction)
  - [Setting Metadata Headers](#setting-metadata-headers)

## Install

Install using nuget:

```sh
dotnet add package Dgraph
```

> WARNING: Be aware that there may be other .NET packages with similar names. To verify the official
> package, please visit https://www.nuget.org/packages/Dgraph. Make sure you are using the correct
> and official package to avoid potential confusion.

## Supported Versions

Depending on the version of Dgraph that you are connecting to, you will have to use a different
version of this client.

| Dgraph version | Dgraph.net version  |
| -------------- | ------------------- |
| dgraph 21.X.Y  | Dgraph.net 21.3.1.2 |
| dgraph 23.X.Y  | Dgraph.net 23.0.0   |

## Using a Client

Most client and transaction methods return a `Result` object from the
[FluentResults](https://github.com/altmann/FluentResults) library. You can check the status of the
result by using `Result.IsSuccess` or `Result.IsFailed`. Exceptions are contained inside
`Result.Errors`.

### Creating a Client

An `IDgraphClient` can be created with a list of `GrpcChannel` as objects. Connecting to multiple
Dgraph servers in the same cluster allows for better distribution of workload.

The following code snippet shows just one connection.

```c#
using Dgraph;
using Grpc.Net.Client;

GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:9080");
using var dgraphClient = DgraphClient.Create(channel);
```

The connection can be configured by using `GrpcChannelOptions`.

```c#
using Grpc.Net.Client;

var options = new GrpcChannelOptions
{
    CompressionProviders = <...>, // List of Grpc ICompressionProvider
    Credentials = Grpc.Core.ChannelCredentials.Create(<...>)
};
GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:9080", options);
```

### Login into a namespace

If your server has Access Control Lists enabled (Dgraph v1.1 or above), the client must be logged in
for accessing data. Use `Login` to obtain and remember access and refresh JWTs.

```c#
var response = await dgraphClient.Login("user", "password");
if (response.IsFailed) {
    // Handle errors
}
```

All subsequent operations via the logged in client will send along the stored access token.

If your server additionally has namespaces (Dgraph v21.03 or above), use `LoginIntoNamespace`.

```c#
var response = await dgraphClient.LoginIntoNamespace("groot", "password", 123);
if (response.IsFailed) {
    // Handle errors
}
```

### Connecting To Dgraph Cloud

Use `DgraphCloudChannel.Create` to create a GrpcChannel that connects to a Dgraph Cloud backend.

`DgraphCloudChannel.Create` can accept GraphQL or gRPC URIs from
[Dgraph Cloud](https://cloud.dgraph.io/), but it will always connect via gRPC.

```c#
using Dgraph;
using Grpc.Net.Client;

string ENDPOINT = "<...>";
string API_KEY = "<...>";

GrpcChannel channel = DgraphCloudChannel.Create(ENDPOINT, API_KEY);
using var dgraphClient = DgraphClient.Create(channel);
```

### Altering the Database

To set the schema, create an instance of `Dgraph.Api.Operation` and use the `Alter` endpoint.

```c#
using Dgraph;

var operation = new Api.Operation {
    Schema = "name: string @index(exact) ."
};
var response = await dgraphClient.Alter(operation);
if (response.IsFailed) {
    // Handle errors
}
```

`Operation` contains other fields as well, including `DropAttr` and `DropAll`. `DropAll` is useful
if you wish to discard all the data without bringing the instance down. `DropAttr` is used to drop
all the data related to a predicate.

Starting in Dgraph version 20.03.0, indexes can be computed in the background. You can set the
`RunInBackground` field to `true` like so:

```c#
using Dgraph;

var operation = new Api.Operation {
    Schema = "name: string @index(exact) .",
    RunInBackground = true
};
var response = await dgraphClient.Alter(operation);
if (response.IsFailed) {
    // Handle errors
}
```

### Creating a Transaction

To create a transaction, call the `IDgraphClient.NewTransaction()` method, which returns a new
`ITransaction`. This operation incurs no network overhead.

To ensure the `ITransaction` is properly disposed after it has completed, use the `using` keyword.

```c#
using var transaction = dgraphClient.NewTransaction();
var transactionResponse = await transaction.Mutate(...);
if (transactionResponse.IsFailed) {
    // Handle errors
}
var response = await transaction.Commit();
if (response.IsFailed) {
    // Handle errors
}
```

Read-only transactions can be created by calling the `IDgraphClient.NewReadOnlyTransaction` method.
Read-only transactions are useful to increase read speed because they can circumvent the usual
consensus protocol. Read-only transactions cannot contain mutations. There is nothing to dispose for
a `ReadOnlyTransaction` object, so it does not implement `IDisposable`.

```c#
var readOnlyTransaction = dgraphClient.NewReadOnlyTransaction();
var response = await readOnlyTransaction.Query(...);
if (response.IsFailed) {
    // Handle errors
}
```

### Running a Mutation

`ITransaction.Mutate` runs a mutation. It takes a `Dgraph.Api.Mutation` or a `MutationBuilder`. You
can set the data using JSON or RDF N-Quad format.

To use JSON, use the fields `SetJson` and `DeleteJson`, which accept a string representing the nodes
to be added or removed respectively (either as a JSON map or a list). You can use any library to
serialize objects to a JSON string, such as [JSON.NET](https://www.newtonsoft.com/json).

To use RDF, use the fields `SetNquads` and `DelNquads`, which accept a string representing the valid
RDF triples (one per line) to be added or removed respectively.

`Dgraph.Api.Mutation` also contains the `Set` and `Del` fields which accept a list of RDF triples
that have already been parsed into our internal format. As such, these fields are mainly used
internally and you should use the `SetNquads` and `DelNquads` fields instead if you plan on using
RDF.

While you can construct a `Dgraph.Api.Mutation` object directly, it is easier to let
`MutationBuilder` handle implementation details like converting `string` to
`Google.Protobuf.ByteString`.

```c#
using Dgraph;

var transaction = dgraphClient.NewTransaction();
var mutation = new MutationBuilder().SetJson("...").CommitNow();
var response = await transaction.Mutate(mutation);
if (response.IsFailed) {
    // Handle errors
}
```

If you want to commit a mutation without querying anything further, use `MutationBuilder.CommitNow`
to indicate that the transaction must be immediately committed.

```c#
var mutation = new MutationBuilder().SetJson("...").CommitNow();
```

Multiple mutations can be run in a single request using `ITransaction.Do` and `RequestBuilder`. To
immediately commit the request, use `RequestBuilder.CommitNow`.

```c#
using Dgraph;

var transaction = dgraphClient.NewTransaction();
var request = new RequestBuilder().WithMutations(
    new MutationBuilder().SetJson("..."),
    new MutationBuilder().SetJson("..."),
    new MutationBuilder().SetJson("..."),
);
var response = await transaction.Do(request);
if (response.IsFailed) {
    // Handle errors
}
```

Keep in mind that if you do not use `RequestBuilder.CommitNow` or `MutationBuilder.CommitNow`, you
will still need to manually commit the transaction using `ITransaction.Commit`.

Check out the example in `source/Dgraph.tests.e2e/TransactionTest.cs`.

### Running a Query

You can run a query by calling `ITransaction.Query`. You will need to pass in a DQL query string. If
you want to pass an additional map of any variables that you might want to set in the query, call
`ITransaction.QueryWithVars` with the variables dictionary as the second argument.

Let’s run the following query with a variable $a:

```c#
var query = @"
  query all($a: string) {
    all(func: eq(name, $a)) {
      name
    }
  }";
var varMap = new Dictionary<string, string> { { "$a", "Alice" } };
var response = await transaction.QueryWithVars(query, varMap);
if (response.IsFailed) {
    // Handle errors
}
```

You can also use `ITransaction.Do` to run a query.

```c#
var query = @"
  query all($a: string) {
    all(func: eq(name, $a)) {
      name
    }
  }";
var varMap = new Dictionary<string, string> { { "$a", "Alice" } };
var request = new RequestBuilder().WithQuery(query).WithVars(varMap);
var response = await transaction.Do(request);
if (response.IsFailed) {
    // Handle errors
}
```

When running a schema query for predicate `name`, the schema response is found in the `Json` field
of the transaction response:

```c#
var query = @"
  schema(pred: [name]) {
    type
    index
    reverse
    tokenizer
    list
    count
    upsert
    lang
  }";
var response = await transaction.Query(query);
if (response.IsSuccess) {
    Console.WriteLine(response.Value.Json);
}
```

### Query with RDF response

You can get query results as a RDF response by calling `ITransaction.QueryRdf`. The `Rdf` field in
the response has the encoded RDF result.

**Note:** If you are querying for only `uid` values, use a JSON format response.

```c#
// Query the balance for Alice and Bob.
var query = @"
  {
    all(func: anyofterms(name, ""Alice Bob"")) {
      name
      balance
    }
  }";
var response = await transaction.QueryRDF(query);
if (response.IsSuccess) {
    // <0x17> <name> "Alice" .
    // <0x17> <balance> 100 .
    Console.WriteLine(response.Value.Rdf);
}
```

`ITransaction.QueryRDFWithVars` is also available when you need to pass values for variables used in
the query.

### Running an Upsert: Query + Mutation

The `ITransaction.Do` method allows you to run upserts consisting of one query and one mutation.
Variables can be defined in the query and used in the mutation.

To know more about upsert, we highly recommend going through the docs at
[Upsert Block](https://dgraph.io/docs/howto/upserts/)

```c#
var query = @"
  query {
    user as var(func: eq(email, ""wrong_email@dgraph.io""))
  }";
var mutation = new MutationBuilder().SetNquads("uid(user) <email> \"correct_email@dgraph.io\" .");
var request = new RequestBuilder()
    .WithQuery(query)
    .WithMutations(mutation)
    .CommitNow();

// Update email only if matching uid found.
var response = await transaction.Do(request);
if (response.IsFailed) {
    // Handle errors
}
```

### Running Conditional Upsert

The upsert block also allows specifying a conditional block using and `@if` directive. The mutation
is executed only when the specified condition is true. If the condition is false, the mutation is
silently ignored.

See more about Conditional Upsert
[here](https://dgraph.io/docs/dql/dql-syntax/dql-mutation/#conditional-upsert).

```c#
var query = @"
  query {
    user as var(func: eq(email, ""wrong_email@dgraph.io""))
  }";
var mutation = new MutationBuilder()
    .Cond("@if(eq(len(user), 1))") // Only mutate if "wrong_email@dgraph.io" belongs to single user.
    .SetNquads("uid(user) <email> ""correct_email@dgraph.io"" .");
var request = new RequestBuilder()
    .WithQuery(query)
    .WithMutations(mutation)
    .CommitNow();

// Update email only if exactly one matching uid found.
var response = await transaction.Do(request);
if (response.IsFailed) {
    // Handle errors
}
```

### Committing a Transaction

A transaction can be committed using the `ITransaction.Commit` method. If your transaction never
submitted any mutations, then `ITransaction.Commit` is not necessary.

An error will be returned if other transactions running concurrently modify the same data that was
modified in this transaction. It is up to the user to retry transactions when they fail.

```c#
using var transaction = dgraphClient.NewTransaction();

// Perform some queries and mutations.

var response = await transaction.Commit();
if (response.IsFailed) {
    // Retry or handle errors
}
```

### Setting Metadata Headers

Metadata headers such as authentication tokens can be set through the `options` argument of gRPC
methods. Below is an example of how to set a header named "auth-token".

```c#
using Grpc.Core;

var metadata = new Metadata
{
    { "auth-token", "the-auth-token-value" }
};
var options = new CallOptions(headers: metadata);
client.Alter(operation, options);
```

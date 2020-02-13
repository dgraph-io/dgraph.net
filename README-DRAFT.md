# dgraph-js [![npm version](https://img.shields.io/npm/v/dgraph-js.svg?style=flat)](https://www.npmjs.com/package/dgraph-js) [![Build Status](https://teamcity.dgraph.io/guestAuth/app/rest/builds/buildType:(id:dgraphjs_Build)/statusIcon.svg)](https://teamcity.dgraph.io/viewLog.html?buildTypeId=dgraphjs_Build&buildId=lastFinished&guest=1) [![Coverage Status](https://img.shields.io/coveralls/github/dgraph-io/dgraph-js/master.svg?style=flat)](https://coveralls.io/github/dgraph-io/dgraph-js?branch=master)

Official Dgraph client implementation for JavaScript (Node.js v6 and above),
using [gRPC].

**Looking for browser support? Check out [dgraph-js-http].**

[grpc]: https://grpc.io/
[dgraph-js-http]: https://github.com/dgraph-io/dgraph-js-http

This client follows the [Dgraph Go client][goclient] closely.

[goclient]: https://github.com/dgraph-io/dgo

Before using this client, we highly recommend that you go through [docs.dgraph.io],
and understand how to run and work with Dgraph.

[docs.dgraph.io]:https://docs.dgraph.io

## Table of contents

  - [Install](#install)
  - [Supported Versions](#supported-versions)
  - [Quickstart](#quickstart)
  - [Using a Client](#using-a-client)
    - [Creating a Client](#creating-a-client)
    - [Altering the Database](#altering-the-database)
    - [Creating a Transaction](#creating-a-transaction)
    - [Running a Mutation](#running-a-mutation)
    - [Running a Query](#running-a-query)
    - [Running an Upsert: Query + Mutation](#running-an-upsert-query--mutation)
    - [Running a Conditional Upsert](#running-a-conditional-upsert)
    - [Committing a Transaction](#committing-a-transaction)
    - [Cleanup Resources](#cleanup-resources)
    - [Debug mode](#debug-mode)
  - [Examples](#examples)
  - [Development](#development)
    - [Building the source](#building-the-source)
    - [Running tests](#running-tests)

## Install

Install using nuget:

```sh
will upload to nuget after review
```

## Supported Versions

This library currently supports version 1.1.X of Dgraph. For older versions, you can use: [this unofficial C# library](https://github.com/MichaelJCompton/Dgraph-dotnet).


## Quickstart

Build and run the [simple][] project in the `examples` folder, which
contains an end-to-end example of using the Dgraph JavaScript client. Follow the
instructions in the README of that project.

## Using a Client

### Creating a Client

Make a new client

```c#
using(var client = DgraphDotNet.Clients.NewDgraphClient()) {
    client.Connect("127.0.0.1:9080");
}
```

Every time client.Connect is called, a new connection is initialized. You can use this to connect to several alphas at once.

### Objects and JSON

Use your favourite JSON serialization library.

Have an object model

```c#
public class Person
{
    public string uid { get; set; }
    public string name { get; set; }
    public DateTime DOB { get; set; }
    public List<Person> friends { get; } = new List<Person>();
}
```

Grab a transaction, serialize your object model to JSON, mutate the graph and commit the transaction.

```c#
using(var transaction = client.NewTransaction()) {
    var json = ...serialize your object model...
    await transaction.Mutate(json);
    await transaction.Commit();
}
```

Or to query the graph.

```c#
using(var transaction = client.NewTransaction()) {
    var res = await transaction.Query(query);
    
    dynamic newObjects = ...deserialize...(res.Value);

    ...
}
```

### Altering the Database

To set the schema, pass the schema into the `client.AlterSchema` function, as seen below:

```c#
var schema = "`name: string @index(exact) .";
var result = client.AlterSchema(schema)

// if (result.isSuccess)
// if (result.isFailed)
```

The returned result object is based on the FluentResults library. You can check the status using `result.isSuccess` or `result.isFailed`. More information on the result object can be found [here](https://github.com/altmann/FluentResults).

`DgraphClient` contains other fields as well, including `DropAll`.
`DropAll` is useful if you wish to discard all the data, and start from a clean
slate, without bringing the instance down.

### Creating a Transaction

To create a transaction, call `DgraphClient#newTransaction()` method, which returns a
new `Transaction` object. This operation incurs no network overhead.

It is good practise to call to wrap the `Transaction` in a `using` block, so that the `Transaction.Dispose` function is called after running
the transaction. 

```c#
using(var transaction = client.NewTransaction()) {
    ...
}
```

### Running a Mutation

`Transaction.Mutate(json)` runs a mutation. It takes in a json mutation string.

We define a person object to represent a person and serialize it to a json mutation string.

```c#
var p = new Person(){ name: "Alice" };

using(var transaction = client.NewTransaction()) {
    var json = ...serialize your object model...
    await transaction.Mutate(json);
    await transaction.Commit();
}
```

Check out the example in `source/Dgraph-dotnet.examples/MutationExample`.

### Running a Query

You can run a query by calling `Transaction.Query(string)`. You will need to pass in a
GraphQL+- query string. If you want to pass an additional map of any variables that
you might want to set in the query, call `Transaction.QueryWithVars(string, Dictionary<string,string>)` with
the variables dictionary as the second argument.

The response would contain the response string.

Let’s run the following query with a variable $a:

```console
query all($a: string) {
  all(func: eq(name, $a))
  {
    name
  }
}
```

Run the query, deserialize the result from Uint8Array (or base64) encoded JSON and
print it out:

```c#
// Run query.
var query = "query all($a: string) {
  all(func: eq(name, $a))
  {
    name
  }
}";
var vars = new Dictionary<string,string>(){{ $a: "Alice" }};
var res = await dgraphClient.NewTransaction().QueryWithVars(query, vars);

// Print results.
Console.Write(res.Value);
```

### Running an Upsert: Query + Mutation

The `txn.doRequest` function allows you to run upserts consisting of one query and one mutation. 
Query variables could be defined and can then be used in the mutation. You can also use the 
`txn.doRequest` function to perform just a query or a mutation.

To know more about upsert, we highly recommend going through the docs at https://docs.dgraph.io/mutations/#upsert-block.

```c#
dgraphClient.Upsert(
  "email",
  GraphValue.BuildStringValue("wrong_email@dgraph.io"),
  "uid <email> \"correct_email@dgraph.io\" .",
  ???
)
```
```js
const query = `
  query {
      user as var(func: eq(email, "wrong_email@dgraph.io"))
  }`

const mu = new dgraph.Mutation();
mu.setSetNquads(`uid(user) <email> "correct_email@dgraph.io" .`);

const req = new dgraph.Request();
req.setQuery(query);
req.setMutationsList([mu]);
req.setCommitNow(true);

// Upsert: If wrong_email found, update the existing data
// or else perform a new mutation.
await dgraphClient.newTxn().doRequest(req);
```


### Committing a Transaction

A transaction can be committed using the `Txn#commit()` method. If your transaction
consisted solely of calls to `Txn#query` or `Txn#queryWithVars`, and no calls to
`Txn#mutate`, then calling `Txn#commit()` is not necessary.

An error will be returned if other transactions running concurrently modify the same
data that was modified in this transaction. It is up to the user to retry
transactions when they fail.

```js
const txn = dgraphClient.newTxn();
try {
  // ...
  // Perform any number of queries and mutations
  // ...
  // and finally...
  await txn.commit();
} catch (e) {
  if (e === dgraph.ERR_ABORTED) {
    // Retry or handle exception.
  } else {
    throw e;
  }
} finally {
  // Clean up. Calling this after txn.commit() is a no-op
  // and hence safe.
  await txn.discard();
}
```


## Examples

- [simple][]: Quickstart example of using dgraph-js.
- [tls][]: Example of using dgraph-js with a Dgraph cluster secured with TLS.

[simple]: ./examples/simple
[tls]: ./examples/tls

## Development

### Running tests

```sh
dotnet test
```
# Qx

Qx is library which extends ASP.NET's SignalR so that clients can query streams using LINQ.

## About Qx

Querying streams feels much like working with SQL and a database. Just like you can express declarative queries that target tables in SQL, you write similar queries using LINQ and Qx to target streams.
The query that you write in your client is sent (at runtime) to the server and continuously evaluated there, and the client only receives the results it cares about.
If you've used LINQ before, particularly if you've used LINQ with IQueryables (LINQ-to-SQL, Entity Framework) or Rx.NET, you'll feel quite comfortable using Qx.

```csharp
var client = await QxAsyncQueryClient.ConnectAsync("http://localhost:60591/queryable-shares");

var query = client.GetEnumerable<SharePrice>("SharePrices")
    .Where(sp => sp.Symbol == "MSFT")

    // Get the market cap
    .SelectMany(sp => client.GetEnumerable<SharesOutstanding>("SharesOutstanding")
        .Where(so => so.Symbol == sp.Symbol)
        .Select(so => ValueTuple.Create(so.Symbol, sp.Price * so.Count)))

    // Find the name
    .SelectMany(mc => client.GetEnumerable<Listing>("Listings")
        .Where(ls => ls.Symbol == mc.Item1)
        .Select(ls => ValueTuple.Create(mc.Item1, ls.Name, mc.Item2)));

await foreach (var element in query)
{
    Console.WriteLine($"{element.Item2} ({element.Item1}): {element.Item3.ToString("C")}");
    // Each element received by the client is the name, symbol and market cap
    // > Microsoft (MSFT): $lots
}
```

I'm building this because I'm interested in exploring the idea of using standing queries like these as a way of creating event-driven applications, this seemed like a first step in that direction.

You should use this when you're using (or considering using) SignalR and you're building something optimized for adaptibility, composability and similar -ilities. I wouldn't use this if you're optimizing for performance.
For example, you might be building a internal-facing stream API for your business and are expecting a bunch of clients to be written against it. Using Qx you can expose some general stream endpoints and let the client developers decide how they want to slice, dice and compose them together.

## Getting started

TODO.
For now, check out Samples/NYSE.

There's two ways you can use Qx with SignalR.

### With inheritance

This is the easiest way to get going though it has some defaults which you might like to consider (such as the names of the endpoint it uses to accept queries).

1. Update your `Startup.cs`'s `ConfigureServices` method to add Qx and configure the Verifier with the members and features you want to allow.
   ```csharp
   services.AddSignalR()
		// Allow anything in our MySchema library (our models/DTOs/etc) so our client can use them in queries
		.AddQx(o => o.WithAllowedMembers(from types in typeof(MySchema.SomeDtoClass).Assembly.GetTypes()
						 from members in types.GetMembers()
						 select members))
   ```
1. Update your Hub to inherit from `QueryableHub<THub>`, which will look like `MyHub : QueryableHub<MyHub>`.
   You pass in the Hub type as a type parameter so we can inspect the Hub methods to discover the available streams.
1. Update your Hub's constructor to accept an `IQxService` and pass it through to the base class, like `public QueryableStocksHub(IQxService qxService) : base(qxService) { }`
   We use this `IQxService` to use the Verifier and Authorizer you configured in your Startup.
1. Update the Hub stream methods you want clients to be able to query to return `IAsyncQueryable<T>` instead of `IAsyncEnumerable<T>`.
   You can add `.AsAsyncQueryable()` to your existing streams to convert them into an `IAsyncQueryable<T>`.

### With a function call

If want a little more customization, you can use the underlying `CompileEnumerableQuery` and `CompileExecutableQuery` function calls yourself.
You can use this to build your own kind of `QueryableHub<THub>` (with your own endpoint names) or you can use this if you're avoiding ASP.NET's dependency injection.


## Security
### Verification

Qx clients are sending code as data which should be an immediate red flag.
The safety comes with the verification of a query that ensures only explicitly allowed features and explictly allowed members (i.e. types, methods, properties, etc.) are contained within the query.
There are predefined groups of members which you can use to allow things like commonly used (but non-buffering) operators, and primitive types and their methods.

See also Limitations.

### Authorization

Qx allows for authorization to be handled per stream. If a query specifies stream 'A' and 'B' then the user needs access to both streams.
There's nothing special here, you can use an `Authorize` attribute or execute your own authorization logic as you would normally on a Hub stream method.

## Limitations

### Integration with SignalR

The integration with SignalR is uhh- lacking. If they add new features, they'll need to be added in Qx too.
A lot of the behaviour needed for a neater integration is encapsulated within SignalR's DefaultHubDispatcher and I'm not familiar enough with it to begin suggesting an alternate design.
What we have now is pretty much a reimplementation of some of that dispatch behaviour.

### Security

#### Verification
Right now members are allowed or they are not, they aren't constrained in any way. This means if you allow something like `AsyncEnumerable.Range` someone could supply `AsyncEnumerable.Range(0, int.Max)`.

There has been no security audit of the query verification or its defaults. I would wait till then before using this in a public-facing API.

#### Authorization

Qx allows for authorization to be handled per stream. If a query specifies stream 'A' and 'B' then the user needs access to both streams.
There's nothing special here, you can use an `Authorize` attribute or execute your own authorization logic as you would normally on a Hub stream method.

### Anonymous types

Anonymous types are used plenty with LINQ, but they are compiler-generated types of which your server will have no idea.
I've opted to 'lower' them to tuples (see the `AnonymousTypeRewriter` for implementation), I'm not sure if that's the best approach but it seems to work.
There are several limitations with the implementation as it stands:
* Anonymous types with more than seven properties are not yet supported. (I need to support using the 'Rest' parameter.)
* Empty anonymous types are not yet supported. (I need to support some kind of unit type.)
* Anonymous types with nested anonymous types are not yet supported. (I need to do some kinda recursion on the rewriting I guess.)

## To-do

### Some basics
* ~~Test ActualHub <: QueryableHub <: Hub inheritance~~
  It works.
* ~~Test n-ary Hub methods (can I have multiple hub methods with varying arguments?)~~
  ~~Nope, but I can use a backtick and a number in a HubMethodName to differentiate.~~
  I don't actually need this.
* ~~First-pass binding where I just lookup what methods are on the Hub and call them,
  skipping authorization.~~
* ~~Test IAsyncQueryable discovery?~~
* ~~Support cancellation of many results~~
  * ~~Why don't I get a cancellation token?~~
    Because of an ASP.NET Core bug.
* ~~Handle authorization (and other SignalR-supported stuff?)~~
  * Bearer tokens https://docs.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-2.2#bearer-token-authentication
    Some fun stuff with JwtBearerEvents.OnMessageReceived
  * AuthorizeAttribute https://docs.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-3.0#authorize-users-to-access-hubs-and-hub-methods
* ~~Handle mismatched arity~~
* ~~Cache the Hub resolution stuff~~
* ~~Consider making authorization service optional~~
  You can create your own queryable hubs!
* ~~Consider moving binders into core project (not SignalR specific?)~~
  Move when needed, internal for now anyway.
* ~~Support type argument for hub client~~
* ~~Add a nice ASP.NET COre style API (builder etc)~~
  ~~No longer required, devs just need to inherit from QueryableHub<>~~
  Added a 'AddQx' builder function.
* ~~Consider making the endpoint names configurable~~
  You can create your own queryable hubs!
* ~~Allow for generators to be preconfigured (e.g. range)~~
* ~~Add a whitelist of allowed types/methods~~
* ~~Pretty printer for expression errors~~
* ~~See if there's a way to do away with the injection of IAuthorizationService etc~~
  Done via IQxService which gets hooked up to the DI system.
* ~~Poke around with introducing a lightweight Either<,> and clean up the CompileQuery() method~~
  ~~Nah, 'coz of prior perf tests. Can't justify it.~~
  Could justify it, made it very lightweight.
* ~~Enable nullable reference types and clean up~~
* ~~Support inlining of local work~~
* ~~Write a bit of a README~~
* Support anonymous types
* Probably replace serializelinq library
* Add JsonConverter or some such to support System.Text.Json
* Support other ways people might create known resource invocations,
  e.g. extensions to a client
* Figure out why the websocket is closing unexpectedly in the sample
* Do I support async IAsyncEnumerable<> HubMethod()s?
* Support Task<IAsyncEnumerable<T>>
* Consider whitelisting members in addition to types, maybe even arguments.
  e.g. Allow Enumerable.Range(*, <= 100);
* Less IEnumerables everywhere for less allocs
* Imagine a Roslyn analyzer which could look at some Qx metadata endpoint and identify what is and isn't allowed.
  (e.g. which members are whitelisted)

### JavaScript support
* Would be lovely

### gRPC.NET support
* Would be lovely
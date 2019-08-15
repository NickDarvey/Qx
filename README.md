# Qx

## To-do
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
* Enable nullable reference types and clean up
* Poke around with introducing a lightweight Either<,> and clean up the CompileQuery() method
* Support inlining of local work
* Support anonymous types
* Support other ways people might create known resource invocations,
  e.g. extensions to a client
* Add JsonConverter or some such to support System.Text.Json
* Figure out why the websocket is closing unexpectedly in the sample
* Probably replace serializelinq library
* Do I support async IAsyncEnumerable<> HubMethod()s?
* Support Task<IAsyncEnumerable<T>>
* Consider whitelisting members in addition to types, maybe even arguments.
  e.g. Allow Enumerable.Range(*, <= 100);
* Less IEnumerables everywhere for less allocs
* Imagine a Roslyn analyzer which could look at some Qx metadata endpoint and identify what is and isn't allowed.
  (e.g. which members are whitelisted)

## Limitations
The integration with SignalR is uhh- lacking. If they add new features, they'll need to be added in Qx too.
A a lot of the behaviour we'd need for a neater integration is encapsulated within SignalR's DefaultHubDispatcher and I'm not familiar enough with it to begin suggesting an alternate design.
What we have now is pretty much a reimplementation of some of that dispatch behaviour.

## Use cases

## Forces
* Composability
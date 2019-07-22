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
* Handle authorization (and other SignalR-supported stuff?)
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
* Figure out why the websocket is closing unexpectedly in the sample
* ~~Add a nice ASP.NET COre style API (builder etc)~~
  No longer required, devs just need to inherit from QueryableHub<>
* ~~Consider making the endpoint names configurable~~
  You can create your own queryable hubs!
* Allow for generators to be preconfigured (e.g. range)
* Add JsonConverter or some such to support System.Text.Json
* Probably replace serializelinq library
* Add a whitelist of allowed types/methods
* Support Task<IAsyncEnumerable<T>>
* Do I support async IAsyncEnumerable<> HubMethod()s?
* Less IEnumerables everywhere for less allocs
* Pretty printer for expression errors
* Support anonymous types


## Limitations
The integration with SignalR is uhh- lacking. If they add new features, they'll need to be added in Qx too.
A a lot of the behaviour we'd need for a neater integration is encapsulated within SignalR's DefaultHubDispatcher and I'm not familiar enough with it to begin suggesting an alternate design.
What we have now is pretty much a reimplementation of some of that dispatch behaviour.

## Use cases

## Forces
* Composability
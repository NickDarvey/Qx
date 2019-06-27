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

* Handle mismatched arity
* Add a nice ASP.NET COre style API (builder etc)
* Cache the Hub resolution stuff
* Support Task<IAsyncEnumerable<T>>

## Limitations
The integration with SignalR is uhh- lacking. If they add new features, they'll need to be added in Qx too.
A a lot of the behaviour we'd need for a neater integration is encapsulated within SignalR's DefaultHubDispatcher and I'm not familiar enough with it to begin suggesting an alternate design.
What we have now is pretty much a reimplementation of some of that dispatch behaviour.
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
* Handle mismatched arity
* Handle authorization (and other SignalR-supported stuff?)
* Add a nice ASP.NET COre style API (builder etc)
* Cache the Hub resolution stuff

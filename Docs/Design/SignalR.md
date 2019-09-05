# SignalR

## Limitations
The integration with SignalR is uhh- lacking. If they add new features, they'll need to be added in Qx too.
A lot of the behaviour needed for a neater integration is encapsulated within SignalR's DefaultHubDispatcher and I'm not familiar enough with it to begin suggesting an alternate design.
What we have now is pretty much a reimplementation of some of that dispatch behaviour.
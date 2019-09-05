# Security

## Verification

Qx clients are sending code as data which should be an immediate red flag.
The safety comes with the verification of a query that ensures only explicitly allowed features and explictly allowed members (i.e. types, methods, properties, etc.) are contained within the query.
There are predefined groups of members which you can use to allow things like commonly used (but non-buffering) operators, and primitive types and their methods.

### Limitations

Right now members are allowed or they are not, they aren't constrained in any way. This means if you allow something like `AsyncEnumerable.Range` someone could supply `AsyncEnumerable.Range(0, int.Max)`.

There has been no security audit of the query verification or its defaults. I would wait till then before using this in a public-facing API.


## Authorization

Qx allows for authorization to be handled per stream. If a query specifies stream 'A' and 'B' then the user needs access to both streams.
There's nothing special here, you can use an `Authorize` attribute or execute your own authorization logic as you would normally on a Hub stream method.
# Concertable.Search.Hosting

Composition-only Aspire helpers for hosts that run Concertable Search Web and Workers. The package exposes
Search resource names and Azure Service Bus topology without referencing the Search runtime implementation.

Search projections consume B2B-owned artist, venue, and concert change/rating events. Producer runtimes and
databases remain outside this package and must be supplied through published contracts and simulator images.

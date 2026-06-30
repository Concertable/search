# Concertable.Search — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## Standalone AppHost cannot replay Customer-origin rating events

`Concertable.Search.AppHost` boots Search alone and replays B2B catalog events
(concert/artist/venue changed) via `Concertable.B2B.Seed.Simulator`. Search *also* subscribes to
rating events (`{artist,venue,concert}ratingupdatedevent`) which originate from Customer reviews,
but there is **no `Concertable.Customer.Seed.Simulator`** to replay them — so a standalone Search run
projects catalog data with no seeded ratings. The host still boots healthy (the subscriptions exist,
they just receive nothing). Fix when a Customer seed simulator is introduced: register it in
`Concertable.Search.AppHost` alongside the B2B one.

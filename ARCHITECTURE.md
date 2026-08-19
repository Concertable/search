# Concertable.Search — Architecture

> Cross-service plan and design rationale: [`api/docs/MICROSERVICES_ARCHITECTURE.md`](../docs/MICROSERVICES_ARCHITECTURE.md)
> Internal module rules: the `module-structure` and `module-structure` skills
> Outstanding gaps: [`TECH_DEBT.md`](./TECH_DEBT.md)

---

## Bounded context

Search owns the anonymous **marketplace read surface**: browse/search, autocomplete, and geo/radius queries over concerts, venues, and artists. It maintains read-optimised projections built **purely from `*.Contracts` integration events** — it has no write endpoints, publishes no events (there is no `Search.Contracts` project), and holds no source-of-truth data.

Search does **not** serve entity-details pages — those are the frozen public wire contract owned by B2B and Customer (`api/AGENTS.md` "DTOs vs Responses"). It is not the canonical catalog (B2B) and not the source of ratings (Customer reviews). Everything in `SearchDb` is a derived projection.

---

## Host topology

| Project | Kind | Purpose |
|---|---|---|
| `Concertable.Search.Web` | ASP.NET Core HTTP host | **Read-side.** Serves the read API; no bus subscription, no projection handlers. Composition root. |
| `Concertable.Search.Workers` | .NET Worker host | **Write-side.** ASB event consumers + projection handlers (`AddSearchProjectionHandlers`) + inbox. |
| `Concertable.Search.Api` | Controllers csproj | Read controllers + API DI, referenced by Web. |
| `Concertable.Search.Application` | Shared csproj | Services, `HeaderType`, keyed factories/dispatcher, DTOs, params, validators. |
| `Concertable.Search.Domain` | Shared csproj | Read-model + rating-projection entities. Depends only on `Concertable.Kernel`. |
| `Concertable.Search.Infrastructure` | Shared csproj | `SearchDbContext`, EF configs, migrations, event handlers, repositories, geo specs. |
| `Concertable.Search.AppHost` | Aspire AppHost | Local-dev orchestrator only. |

**Database:** `SearchDb` (SQL Server), schema `search` — table names in `Infrastructure/Schema.cs`. Workers always migrates `SearchDbContext` + the messaging `InboxDbContext` on startup (app-lock guarded); Web migrates `SearchDbContext` only when not Production.

---

## Read model + rating projections — separate tables, LEFT-joined at read time

The catalog read models and the rating projections are **distinct tables**, never denormalized together:

| Read model | Rating projection (separate table) |
|---|---|
| `ArtistReadModel` (+ `ArtistReadModelGenre`) | `ArtistRatingProjection` (`{ ArtistId, AverageRating, ReviewCount }`) |
| `VenueReadModel` | `VenueRatingProjection` |
| `ConcertReadModel` (+ `ConcertReadModelGenre`) | `ConcertRatingProjection` |

Read models carry **no** rating field. Reads LEFT-join the rating projection at query time (`Queryable{Artist,Venue,Concert}HeaderMappers` — `join … into … from … DefaultIfEmpty()`), so a projection with no rating yet reads as `null` rather than a stale denormalized zero. This is why the catalog and rating events land in different tables and never race to write one row.

---

## Integration events — consumed only, from B2B contracts

Search subscribes to six B2B-owned events (Workers `Program.cs`); each handler (`Infrastructure/Handlers/`) writes one projection table.

| Event | Defined in | Handler → table |
|---|---|---|
| `ArtistChangedEvent` | `Concertable.B2B.Artist.Contracts.Events` | `ArtistProjectionHandler` → `Artists` |
| `VenueChangedEvent` | `Concertable.B2B.Venue.Contracts.Events` | `VenueProjectionHandler` → `Venues` |
| `ConcertChangedEvent` | `Concertable.B2B.Concert.Contracts.Events` | `ConcertProjectionHandler` → `Concerts` |
| `ArtistRatingUpdatedEvent` | `Concertable.B2B.Artist.Contracts.Events` | `ArtistRatingProjectionHandler` → `ArtistRatingProjections` |
| `VenueRatingUpdatedEvent` | `Concertable.B2B.Venue.Contracts.Events` | `VenueRatingProjectionHandler` → `VenueRatingProjections` |
| `ConcertRatingUpdatedEvent` | `Concertable.B2B.Concert.Contracts.Events` | `ConcertRatingProjectionHandler` → `ConcertRatingProjections` |

**Ratings originate in Customer** (`CustomerReviewSubmittedEvent`), but B2B consumes that, recomputes the average, and **re-publishes its own** `*RatingUpdatedEvent`. Search binds to those B2B contracts and **never references Customer's contracts or runtime** — every consuming csproj pins the B2B contracts as a `PackageReference` marked *"Never a ProjectReference: would break Search's standalone carve."* This keeps Search a data service that depends on no other data service's runtime (`api/AGENTS.md`).

Every handler is idempotent: inbox dedup on `(MessageId, ConsumerName)` (`DbContextBase.IsInboxMessageProcessedAsync`) plus upsert-by-id (find-then-insert-or-mutate; genre child sets reconciled by diff).

---

## Read API

Controllers (`Api/Controllers/`, mostly `[AllowAnonymous]`):

- `HeaderController` — `GET api/Header?headerType={artist|venue|concert}&…` browse/search + `…/amount/{n}`.
- `ConcertHeaderController` — `popular`, `free` (anon), `recommended` (`[Authorize]`).
- `AutocompleteController` — `GET api/Autocomplete?headerType={enum?}&searchTerm=`; a null `headerType` selects the all-types service.

`HeaderType` (`Application/HeaderType.cs`, values `Artist`/`Venue`/`Concert`) keys the read side via the codebase's keyed-strategy-resolver pattern: `AddKeyedScoped<IHeaderService, …>(HeaderType.X)` + `HeaderServiceFactory`/`HeaderDispatcher` resolve the per-type service; `IHeader` DTOs are `[JsonDerivedType]`-polymorphic on the same key. Geo/radius uses **NetTopologySuite** (`GeometrySpecification`: WGS84 `Point.Distance(center) <= radiusMeters`, default 10 km); read models are `IHasLocation` with `geography` columns.

---

## Tech stack

.NET 10 · EF Core + SQL Server (`SearchDbContext : DbContextBase`) · NetTopologySuite (`geography`) · Azure Service Bus (Workers) · `Concertable.Messaging` (Inbox/Transport) · Aspire (`Concertable.ServiceDefaults`) · JWT Bearer (audience `concertable.search.api`; most reads anonymous) · `Concertable.Shared.Api` · LinqKit (`.AsExpandable()` in header mappers) · FluentValidation.

The standalone `Concertable.Search.AppHost` replays B2B catalog events via `Concertable.B2B.Seed.Simulator`; there is no Customer seed simulator, so a standalone run has catalog data but no seeded ratings — see [`TECH_DEBT.md`](./TECH_DEBT.md).

---

## What is NOT in this service

| Concern | Lives in |
|---|---|
| Canonical concert/venue/artist writes, workflow | `Concertable.B2B` |
| Ratings source of truth (reviews) | `Concertable.Customer` |
| Public entity-details pages (frozen wire contract) | `Concertable.B2B` / `Concertable.Customer` |
| Ticket sales, customer profile/preferences | `Concertable.Customer` |
| Payments, payout ledger | `Concertable.Payment` |
| Identity authority (`sub`, tokens) | `Concertable.Auth` |

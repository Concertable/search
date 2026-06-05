# Concertable.Search — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## MED

### Read-model `Location` is nullable despite non-nullable producer contracts

`ConcertReadModel`, `VenueReadModel`, and `ArtistReadModel` all declare `Point? Location` (columns `nullable: true`), but every production writer guarantees a value: `ConcertChangedEvent` / `VenueChangedEvent` / `ArtistChangedEvent` carry non-nullable `double Latitude/Longitude` (B2B's `VenueEntity`/`ArtistEntity` domain-validate `Location` non-null at the source — a venue cannot exist without one), so the projection handlers bind to the non-nullable `CreatePoint(double, double)` overload, and the seed mappers always construct a `Point`. Null is unrepresentable in the data yet representable in the type and schema. The query side then compensates with guards that **silently drop rows** instead of failing loudly: `QueryableConcertHeaderMappers` (`where v.Location != null`), `QueryableVenueHeaderMappers`, and `GeometrySpecification` (`e.Location != null && …`) — a projection bug would make an entity vanish from search results without a trace. Root cause is Kernel's `IHasLocation.Location` being `Point?`; once B2B deletes its two dead implementers (see `api/Concertable.B2B/TECH_DEBT.md`), every remaining implementer is one of these read models with a guaranteed location.

**Resolves when:** the three read models declare non-nullable `Point Location` with `IsRequired()` EF configs (migration re-scaffold via `./initial-migrations.ps1`), Kernel's `IHasLocation.Location` becomes non-nullable `Point`, the `Location != null` guards are stripped from `GeometrySpecification` and the queryable header mappers, and the `Apply_ShouldFilterOutEntities_WhenLocationIsNull` unit test is deleted.

The rule after the fix: implementing `IHasLocation` *guarantees* a non-null location (geometry-queryable); a type whose location is genuinely optional (`UserEntity`) must not implement the interface.

---

## LOW

### `ConcertReadModel.Price` missing `HasPrecision` in EF config

EF warns at migration scaffold time: "No store type was specified for the decimal property 'Price' on entity type 'ConcertReadModel'. This will cause values to be silently truncated if they do not fit in the default precision and scale."

**Resolves when:** `ConcertReadModelConfiguration` (or equivalent `IEntityTypeConfiguration`) calls `.HasPrecision(18, 2)` (or `HasColumnType("decimal(18,2)")`) on the `Price` property.

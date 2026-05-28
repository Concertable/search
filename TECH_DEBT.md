# Concertable.Search — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## LOW

### `ConcertReadModel.Price` missing `HasPrecision` in EF config

EF warns at migration scaffold time: "No store type was specified for the decimal property 'Price' on entity type 'ConcertReadModel'. This will cause values to be silently truncated if they do not fit in the default precision and scale."

**Resolves when:** `ConcertReadModelConfiguration` (or equivalent `IEntityTypeConfiguration`) calls `.HasPrecision(18, 2)` (or `HasColumnType("decimal(18,2)")`) on the `Price` property.

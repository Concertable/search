# Code review — Refactor/PostgresAuthConsumerSweep

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `849f05c32704459b8c50af87827a456f6e952196`  `(2026-09-20)`
**Judgment:** `approved`

## Review pass — 2026-09-20 — composition

**Candidate base:** `40878192`
**Candidate branch:** `Refactor/PostgresAuthConsumerSweep`
**Candidate scope:** `all`
**Candidate paths:** `Directory.Packages.props`, `local/AppHost/AppHost.cs`,
`api/tests/Concertable.Search.StartupTests/ResourceGraphTests.cs`,
`reviews/Refactor-PostgresAuthConsumerSweep.md` `(4 paths)`
**Work-order path:** `reviews/Refactor-PostgresAuthConsumerSweep.md`
**Work-order mode:** `new`
**Pass judgment:** `approved`

The plan ledger's Next Steps step 2, for Search: the last SQL Server resource in this repository existed
only to host `AuthDb` for a pre-cut-over `auth` image. Auth's cut-over published a PostgreSQL image and a
migrations image, so the container goes.

### Findings

No findings.

### Verified

- **The platform bump is forced, not opportunistic.** `Concertable.Auth.Hosting 0.2.0-alpha.0.305`
  declares dependencies on `Concertable.AppHost.Shared 0.2.0-alpha.0.14` and
  `Concertable.Shared.Email.Application 0.2.0-alpha.0.14` (read from the published nuspec), so central
  package management cannot hold the platform at `0.13` alongside it. Platform `0.14` is
  [platform-dotnet #12](https://github.com/Concertable/platform-dotnet/pull/12), "Remove ambient tenant
  host bypass", touching `TenantInterceptor`, `ITenantContext` and their tests. Search references none of
  those symbols — a tree-wide grep for `ITenantContext`, `TenantInterceptor` and `ITenantScoped` finds
  nothing — so the bump is inert here beyond satisfying the graph.
- Both auth digests were resolved from GHCR **by the exact commit tag** `0f80b89c...`, not by recency,
  because each Publish run pushes an `e2e-` variant seconds later that would otherwise win: `auth`
  `sha256:cbd7c429...`, `auth-migrations` `sha256:090b1bb8...`.
- `AddAuth` at `0.305` wires `WaitForCompletion(migrations)` itself, so the AppHost correctly adds no
  trailing `WaitForCompletion` of its own — the same shape System uses, and deliberately unlike Payment,
  Search and B2B's own migration resources, which order at the call site.
- `AuthDb` moves onto the existing `concertable-search-postgres-data` server. Auth needs no PostGIS and no
  `max_prepared_transactions`; sharing Search's PostGIS-enabled server is harmless and matches System,
  which hosts `authDb` on the same server as `searchDb`, `b2bDb` and `paymentDb`.
- The auth resource's `--user root` argument, its endpoint named `https` and the developer-certificate
  bridge are all unchanged. The image's endpoint behaviour did not change across the cut-over, so this
  slice deliberately leaves that alone; `ResourceGraphTests` still pins all three.
- Nothing else in the repository referenced the `sql` container, `AuthDb` or the superseded auth digest.

### Test change

`ResourceGraphTests.ProductionGraphAndStrictValidation_AreValid` gains the contract this slice
establishes, mirroring what it already pins for Search's own migrations: `AuthDb` is a
`PostgresDatabaseResource`, `auth-migrations` waits on it until healthy, `auth` waits for that job's
completion, and **no `SqlServerServerResource` exists in the graph at all**. That last assertion is what
stops the container coming back.

## Second pass — 2026-09-20 — CI (`849f05c3`)

The first push went red in CI and stayed green locally, twice each on the same commit. It failed at 1m13s
against a five-minute budget, so nothing timed out — a resource died, and Aspire names only the resource
that was waited on, so `search-web` wore the blame for a dependency it waits on.

Cause: `auth-migrations` is a package created 2026-09-20 linked to `Concertable/auth`. A private package
grants its own repository's Actions access and nothing grants another's, so this repository's
`GITHUB_TOKEN` cannot pull it — while the `auth` image beside it predates the repository split and is
still readable, which is why the old composition worked. Fixed by logging in to GHCR with the org packages
token this job already holds for the NuGet feed, which is what `system/qualify.yml` does for the same
reason. Confirmed: CI green at `849f05c3`.

The second half of that commit is the diagnostic — re-throwing with every resource's state and exit code.
Aspire's own message cannot name the resource that actually died, and without this the only way to tell a
pull failure from a migration failure is to guess. It is in this PR rather than a follow-up because this
slice is what put a run-to-completion resource on the critical path.

### Gates

Release build 0 warnings / 0 errors. Startup 8/8, architecture 7/7, unit 17/17, integration 47/47, E2E
1/1 — the E2E run boots the real composition, so it is direct proof that the published `auth` and
`auth-migrations` images create and migrate `AuthDb` on PostgreSQL with no SQL Server present. Remote CI
green at `849f05c3`.

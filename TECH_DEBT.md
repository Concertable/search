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

## D001 — StartupTests resolves conflicting EF Core assemblies

- **Kind:** debt.
- **Observed/reported:** 2026-10-18. `artifacts/efcore-build.log` records a successful Release build with `MSB3277` conflicts between `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` 10.0.12 and 10.0.13 while building `Concertable.Search.StartupTests`. The current StartupTests restore assets contain 10.0.12, while Search.Workers restore assets contain 10.0.13. The source of the 10.0.12 reference is not yet confirmed.
- **Impact and priority:** The build succeeds, but the test host resolves different EF Core assembly versions across project references; P2 because the conflict creates a risk of version-dependent startup or test behavior and leaves a warning in the full build.
- **Source owner:** Search dependency graph, centered on `Concertable.Search.StartupTests` and its referenced projects.
- **State:** open.
- **Repair link:** none.
- **Next action:** Trace the StartupTests restore and assembly-reference graph to identify the source of EF Core 10.0.12, then align the graph.
- **Resolution condition:** A clean Release build of StartupTests and the full Search solution produces no `MSB3277` EF Core or Relational version-conflict warnings, and the resolved package graph uses one EF Core version.
- **Verification evidence:** `artifacts/efcore-build.log` contains the successful build and both `MSB3277` warnings; `api/tests/Concertable.Search.StartupTests/obj/project.assets.json` contains 10.0.12 and `api/src/Concertable.Search.Workers/obj/project.assets.json` contains 10.0.13.

## D002 — Integration tests use mixed assertion libraries

- **Kind:** debt.
- **Observed/reported:** 2026-10-18. The active `Refactor/EfCoreSpecifications` candidate adds a Shouldly 4.3.0 reference to Search's integration-test project and uses Shouldly in `PostgresProviderTests.cs`. Existing integration tests such as `ConcertHeaderApiTests.cs` and `AutocompleteApiTests.cs` still use xUnit `Assert` throughout. This mixed convention is actionable after the Search candidate merges.
- **Impact and priority:** The integration suite has inconsistent assertion style and diagnostics; P3 because this is a maintainability issue with no observed runtime impact.
- **Source owner:** Search integration tests.
- **State:** deferred; the existing assertions were left in place to keep the current EF Core specification/query-composer refactor scoped to its behavior change.
- **Publication dependency:** Merge the `Refactor/EfCoreSpecifications` Search code candidate before merging this debt record to the default branch, so the recorded Shouldly dependency exists there.
- **Reason:** The current refactor introduces Shouldly for new provider-regression assertions but does not include a repository-wide test assertion migration.
- **blocked_by:** conflict.
- **Conflict:** The Search refactor owner controls `Refactor/EfCoreSpecifications`, including `plans/efcore-patterns/PLAN.md` and `api/tests/Concertable.Search.IntegrationTests/PostgresProviderTests.cs`.
- **Resolver:** The Search refactor owner.
- **Resume condition:** The active refactor has merged, making Shouldly available on the default branch, or the Search owner opens a focused assertion-convention change.
- **Repair link:** none; the active refactor plan is not yet on the default branch.
- **Next action:** Migrate the existing Search integration-test assertions from xUnit `Assert` to Shouldly in a focused change.
- **Resolution condition:** Search integration tests use Shouldly consistently and the integration-test suite passes.
- **Verification evidence:** `api/tests/Concertable.Search.IntegrationTests/Concertable.Search.IntegrationTests.csproj` references Shouldly 4.3.0; `PostgresProviderTests.cs` imports Shouldly, while existing integration test files still contain xUnit `Assert` calls.

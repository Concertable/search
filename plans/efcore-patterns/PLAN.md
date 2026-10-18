# Canonical EF Core specifications and query builders

## Outcome and authority
User authorized updating Search specifications and DI query building, researching the naming, creating a canonical generic EF Core specification/query-building standard, and moving other EF Core-specific .NET skills into efcore. Existing repository delivery authorization applies after review and checks.

## Decisions and completed assessment
Specifications describe one operation, capture concrete input values in constructors, and are passed as parameters rather than resolved through DI. Reuse the published Kernel predicate/composition and DataAccess ordering APIs; do not introduce Ardalis or another parallel framework. Builders compose IQueryable and are named *QueryBuilder; repositories execute and materialize. Builders inject geometry/time dependencies and resolve them once per Build. Specifications do not retain mutable request objects or services.
Research assessment complete: retain existing shared framework and refactor its consumers. Adoption/forking a separate specification framework adds a duplicate abstraction without filling a missing capability. Microsoft describes query specifications as objects carrying query input; Ardalis distinguishes specifications, fluent builders and evaluators. These sources establish the distinction; QueryBuilder is our explicit house naming for a DI composer, not an EF Core framework type.
- https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core
- https://specification.ardalis.com/usage/create-specifications.html
- https://specification.ardalis.com/usage/use-specification-repository-pattern.html

## Delivery slices
1. Search atomic consumer refactor: specification constructors, query-builder names, DI registrations, repositories and affected tests/docs. Base origin/main; branch Refactor/EfCoreSpecifications. Expected fewer than 40 substantive files / 1000 substantive lines; reassess if exceeded. Validate build, unit and real PostgreSQL integration tests plus existing CI.
2. Generic standards: independent source owner tj-agents/dotnet; isolated agent-owned checkout, namespace migration, harness/catalog/routes/generated settings and focused generation tests. Does not require a Search package release.

## Progress
- Search branch refreshed from origin/main before implementation; pre-existing untracked .codex/agents preserved.
- Inspected singleton specification registrations and all composer/repository callers. Existing provider tests cover case/accent/collation and PostGIS translation.
- Standards slice delegated to efcore_standards with disjoint source ownership.
- Shell sandbox helper cannot launch (setup refresh errors); approved escalated host shell works. Node filesystem reads also work.

## Acceptance and next action
Preserve filtering, defaults, ordering, Unicode/collation and spatial SQL behavior. Add regression coverage for isolation between requests and clock capture; no specifications registered with DI. Complete generated standards checks and remove old namespace references in supported authored sources. Update repository plugin selection through the owning installer once efcore is published. Next: implement Search value specifications and DI builders, then run focused validation and review. Record any actual external blocker and its resolver here.

## Search implementation checkpoint
- Replaced DI predicate factories with value-bound Kernel specifications, copied genre arrays and spatial points, and bound the UTC clock once per query build.
- Renamed Queries/IQuery to QueryBuilders/IQueryBuilder and updated all repository callers and DI registrations. Sort descriptions now implement the existing shared ordered-specification contract.
- Release solution build passed (existing ASPIRE010/MSB3277 warnings being reconciled separately). Unit 13/13, architecture 7/7 and startup 8/8 passed. PostgreSQL integration run pending.
- Scope assessment: this atomic refactor includes removed DI interfaces/composites and all construction sites; splitting the deletions from callers would break compilation. Most file count is mechanical renaming/deletion; substantive delta remains well below 1000 lines.
- Standards source branch Refactor/EfCorePatterns at .worktrees/efcore-standards. Marketplace publication is a separate required dependency. Validation-debt records have a disjoint docs owner; exclude them from this code commit.

## Publication gate and final preparation
- Search code commit 1427269e261f428dc0199d4b5c228a2028ec6eaf passed native review and the checks above; full PostgreSQL suite passed 48/48. Final paging alignment appends Id tie-breakers to nonunique primary orders; focused isolation/paging provider regressions passed 2/2.
- Source standards candidate 088e21f5ceb722c649dbb5155f3b2bb2dcc6802c passed 82 tests (2 optional external-core-checkout checks skipped) and 759 generated-output checks. Native independent review found a packaged snippet-source resolution regression; its source owner is repairing it before final review.
- Marketplace branch Refactor/EfCoreMarketplace at f26a1e57c286dcbb9238a8143e5d5e5ad6c398d5 and Core branch Refactor/EfCorePluginRoster at ea64112107f45a159f6d0b616f41ce66d9ad9fe8 have clean native reviews and passing focused generation/installer tests. Checkouts: .worktrees/efcore-marketplace and .worktrees/efcore-core.
- Search future plugin selection/settings and efcore-service routes were generated and checked using the prepared source installer. Their snapshots are retained under artifacts/efcore-consumer-selection. Activation was reverted in the active checkout because the unpublished plugin cannot load yet; no caches were edited or plugins installed. Regenerate from published source after source delivery.
- Automatic approval review rejected git push and draft PR creation for Concertable/search: user must explicitly authorize exporting the private branch payload and creating GitHub review state. This is a publication gate, not a local implementation gate. No publication retry or workaround is authorized. Resolver: user. Unblock action: approve push/PR/merge chain for Search and tj-agents/{dotnet,marketplace,core}. Resume condition: explicit approval.
- Next after approval: publish and validate standards producer, then marketplace and Core roster; publish/validate Search code, adopt and check efcore plugin settings/routes once available, review the generated consumer delta, then land through required checks. Debt docs branch Docs/SearchValidationDebt follows Search code because its assertion-library evidence is from this candidate.

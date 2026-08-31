# Concertable.Search

The **Search** service of [Concertable](https://github.com/Concertable/concertable) — the search
*data service*: it maintains read-optimised projections of concerts, venues, artists, and ratings,
built purely from B2B-owned `*.Contracts` integration events. Customer reviews feed rating
aggregation upstream in B2B; Search consumes the resulting B2B-owned rating events. It never
depends on another data service's runtime. When a standalone host lacks B2B events at seed time,
the B2B-owned seed simulator replays them.

## Promotion staging

This private repository is the writable Search extraction and promotion-staging repository. Search
promotion preparation is developed through pull requests here. The monorepo remains canonical until
the ordered cutover gates are complete and the final rename, publication, and source removal are
explicitly authorized.

## Building the independent closure

The deployable closure consumes Concertable's shared platform and cross-service contracts as NuGet
`PackageReference`s from the private org feed `https://nuget.pkg.github.com/Concertable`. Restoring
them needs a GitHub [personal access token](https://github.com/settings/tokens) with the
**`read:packages`** scope, exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet build src/Concertable.Search.Web/Concertable.Search.Web.csproj --configuration Release
dotnet build src/Concertable.Search.Workers/Concertable.Search.Workers.csproj --configuration Release
dotnet test tests/Concertable.Search.UnitTests/Concertable.Search.UnitTests.csproj --configuration Release
dotnet test tests/Concertable.Search.IntegrationTests/Concertable.Search.IntegrationTests.csproj --configuration Release
```

The integration suite requires Docker. The repository CI supplies its `GITHUB_TOKEN` through
`GITHUB_PACKAGES_TOKEN` and runs these same four gates. The source-coupled AppHost,
ArchitectureTests, and inherited full-stack E2E helper are intentionally outside this preparation
slice until their RT3 and Stage 4 seams are available.

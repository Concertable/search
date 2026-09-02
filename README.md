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
dotnet restore Concertable.Search.slnx
dotnet build Concertable.Search.slnx --configuration Release --no-restore
dotnet publish src/Concertable.Search.Migrations/Concertable.Search.Migrations.csproj --configuration Release
dotnet pack src/Concertable.Search.Hosting/Concertable.Search.Hosting.csproj --configuration Release --output artifacts/packages -p:MinVerVersionOverride=0.0.0-local
dotnet pack src/Concertable.Search.TestKit/Concertable.Search.TestKit.csproj --configuration Release --output artifacts/packages -p:MinVerVersionOverride=0.0.0-local
pwsh ./scripts/verify-package-candidates.ps1 -PackageDirectory artifacts/packages
dotnet test Concertable.Search.slnx --configuration Release --no-build --no-restore -m:1
```

The integration suite requires Docker. The repository CI supplies its `GITHUB_TOKEN` through
`GITHUB_PACKAGES_TOKEN`, runs these repository gates, packs the unpublished Hosting and TestKit candidates,
verifies both through one clean installed-package consumer, builds the standalone AppHost, runs its
architecture coverage, and retains preparation artifacts for seven days. The standalone host runs Search
from source, consumes Auth and the B2B seed simulator as digest-pinned images, and does not provision a
foreign data-service database. It runs the Search migration job to successful completion before starting Web
or Workers; those runtime hosts never modify schema. CI logs into GHCR with its ephemeral repository token,
then the Search-owned standalone test proves that B2B simulator events rebuild artist, venue, and concert
projections observable through Search's public autocomplete API. The inherited full-stack E2E helper remains
system-owned and excluded.

## Building container candidates

The repository Dockerfile has separate non-root targets for Web, Workers, and the migration job. Package
credentials are exposed only to the restore instruction as a BuildKit secret and are not stored in an image
or build argument:

```sh
docker build --target search-web --build-arg BUILD_VERSION=0.0.0-local --secret id=GITHUB_PACKAGES_TOKEN,env=GITHUB_PACKAGES_TOKEN -t concertable-search-web:local .
docker build --target search-workers --build-arg BUILD_VERSION=0.0.0-local --secret id=GITHUB_PACKAGES_TOKEN,env=GITHUB_PACKAGES_TOKEN -t concertable-search-workers:local .
docker build --target search-migrations --build-arg BUILD_VERSION=0.0.0-local --secret id=GITHUB_PACKAGES_TOKEN,env=GITHUB_PACKAGES_TOKEN -t concertable-search-migrations:local .
```

These commands build local candidates only. Publication, canonical tags, and visibility changes remain part
of the later approved cutover.

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
pwsh ./scripts/verify-package-candidates.ps1 -PackageDirectory artifacts/packages
dotnet test Concertable.Search.slnx --configuration Release --no-build --no-restore -m:1
```

The integration suite requires Docker. The repository CI supplies its `GITHUB_TOKEN` through
`GITHUB_PACKAGES_TOKEN`, runs these repository gates, packs the unpublished Hosting candidate,
verifies it through one clean installed-package consumer, builds the standalone AppHost, runs its
architecture coverage, and retains preparation artifacts for seven days. The standalone host runs Search
from source, consumes Auth and the B2B seed simulator as digest-pinned images, and does not provision a
foreign data-service database. It runs the Search migration job to successful completion before starting Web
or Workers; those runtime hosts never modify schema. CI logs into GHCR with its ephemeral repository token,
then the Search-owned standalone test proves that B2B simulator events rebuild artist, venue, and concert
projections observable through Search's public autocomplete API. The inherited full-stack E2E helper remains
system-owned and excluded.

## Building container candidates

The repository Dockerfile uses digest-pinned build and runtime bases and has separate non-root targets for
Web, Workers, and the migration job. Each target records the canonical source repository, exact revision,
and candidate version as OCI metadata. Package credentials are exposed only to the restore instruction as
a BuildKit secret and are not stored in an image or build argument.

Use the repository verifier to build and inspect all three local candidates. It refuses to overwrite existing
tags, checks their runtime contract and metadata, checks that the package credential was not retained, runs
runtime smoke checks, and removes only images labelled as owned by that verification run:

```sh
pwsh ./scripts/verify-search-images.ps1 -BuildVersion 0.0.0-local
```

CI also scans the source and local images for secrets, blocks critical image vulnerabilities, and validates a
CycloneDX SBOM for every target. These checks build local candidates only. They do not push images, create
canonical tags, or change package/image visibility; publication remains owned by the later organization-level
cutover workflow.

## Verifying one release candidate

The combined verifier proves that the Hosting package and all three images come from one clean
Search revision and share one MinVer-derived version. It exercises the clean package consumer and image
contracts, scans saved image archives, creates checksummed scan/SBOM evidence, and validates a complete release
manifest before removing its owned temporary directory, images, and Docker cache volume:

```sh
pwsh ./scripts/verify-search-release-candidate.ps1
```

Pass `-KeepArtifacts -OutputPath <path-outside-the-repository>` to inspect the verified bundle locally. The
bundle records intended repositories and tags but contains no registry or NuGet publisher; verification cannot
push it. CI runs this combined rehearsal in addition to the normal build/test gate.

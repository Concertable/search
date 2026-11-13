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

## Run Search locally

This is a local-development composition, not a cloud deployment or product publication. It runs Search
from source with a shared PostgreSQL/PostGIS server (`SearchDb` and `AuthDb`), the Azure Service Bus
emulator, digest-pinned Auth and Auth-migrations images, and the digest-pinned B2B seed simulator. It
does not start a B2B database or B2B runtime.

### Prerequisites

- .NET SDK 10 and an Aspire CLI compatible with the AppHost SDK (the composition is currently built with
  Aspire 13.6.2). Install the CLI normally and make `aspire` available on `PATH`; confirm the version with
  `aspire --version`.
- A running Docker Engine configured for **Linux containers**. The emulator, PostgreSQL/PostGIS, Auth, and
  the B2B simulator are containers.
- A trusted HTTPS development certificate for the Auth image bridge:

  ```powershell
  dotnet dev-certs https --trust
  ```

- GitHub Packages access for restore: a GitHub PAT with `read:packages`, supplied without printing it as
  `GITHUB_PACKAGES_TOKEN`. The repository's `nuget.config` uses that variable for `Concertable.*` packages.
- GHCR pull access for the private Auth and B2B images. Sign in interactively before the first run:

  ```powershell
  docker login ghcr.io
  ```

  Use a GitHub account/PAT authorised to read the organisation's packages; do not put the token in a
  command line, source file, or checked-in settings.

### AppHost secrets

The AppHost has `UserSecretsId` `search-apphost-dev-secrets`. On a normal run it generates and persists
three secret parameters of at least 32 characters, then supplies them to Auth as its B2B, Customer, and
service client secrets. `concertable-auth` remains the Auth client ID; no SPA clients are enabled, and the
developer-certificate bridge remains part of the Auth resource.

For an existing setup, before the first wrapper run with this change, copy each effective `ServiceAuth` user-secret value to its
parameter key. Do not print, log, or silently rotate these secrets during the migration.

| Obsolete key | Parameter key |
| --- | --- |
| `ServiceAuth:B2BClientSecret` | `Parameters:auth-b2b-client-secret` |
| `ServiceAuth:CustomerClientSecret` | `Parameters:auth-customer-client-secret` |
| `ServiceAuth:AuthClientSecret` | `Parameters:auth-service-client-secret` |

After migrating the values, remove the obsolete keys. Actual reconciliation with retained Auth state remains
a runtime acceptance gate.

Keep the generated defaults. If an authorised rotation is needed, set the relevant AppHost user-secret key
to a new value of at least 32 characters; the keys are:

```powershell
dotnet user-secrets set "Parameters:auth-b2b-client-secret" "<new 32+ character secret>" --project ./local/AppHost/Concertable.Search.AppHost.csproj
dotnet user-secrets set "Parameters:auth-customer-client-secret" "<new 32+ character secret>" --project ./local/AppHost/Concertable.Search.AppHost.csproj
dotnet user-secrets set "Parameters:auth-service-client-secret" "<new 32+ character secret>" --project ./local/AppHost/Concertable.Search.AppHost.csproj
```

### Start, observe, and stop

The supported entry point first runs the qualified local Infra lab, requires its fresh `passed` receipt, then
starts Search in the foreground. `-InfraPath` is explicit: the required source revision
`f331b58443cd4c701174039bad1ff10cc298b8b0` is not published on a remote branch, so a fresh clone cannot
qualify it. Obtain the qualified local Infra checkout from its owner. The wrapper temporarily removes
`ARM_*` and `AZURE_*` credential variables for the lab process and restores the caller environment before
starting Search.

From the repository root:

```powershell
pwsh ./scripts/start-local.ps1 -InfraPath '<absolute path to the qualified Infra checkout>'
```

If the native Aspire CLI is not on `PATH`, supply its qualified command path with `-AspireCommand`. The
wrapper refuses an unexpected Infra repository, a lab changed from the pinned revision, an existing lab
container/network, an existing Search AppHost, failed lab execution, or a stale/non-passing receipt. It does
not stop a workload to clear any of those conditions.

Starting from a different Search worktree can select a different derived Postgres volume. This entry point
does not migrate or reuse historical lesson data: named and anonymous retained volumes stay preserved, and
an authorised backup-and-verified-restore transition remains the gate before changing them.

Use `Ctrl+C` to end the foreground Search run. The AppHost orders Auth migrations before Auth, and Search
migrations before both Search Web and Workers. The B2B simulator waits for the workers, then publishes its
catalog events and exits. Launch alone is not a completed setup: check the resources and autocomplete path
below.

For a detached run that you have identified as this AppHost, stop only that AppHost:

```powershell
aspire stop --apphost ./local/AppHost/Concertable.Search.AppHost.csproj
```

The following Aspire 13.6.1 CLI diagnostics are useful while the app is running:

```powershell
aspire describe --apphost ./local/AppHost/Concertable.Search.AppHost.csproj
aspire describe search-web --apphost ./local/AppHost/Concertable.Search.AppHost.csproj
aspire logs search-migrations --apphost ./local/AppHost/Concertable.Search.AppHost.csproj --tail 200
aspire logs search-workers --apphost ./local/AppHost/Concertable.Search.AppHost.csproj --tail 200
```

Check the resource state before interpreting a request: `search-migrations` must complete successfully;
Auth must have completed its migration and be discoverable; and `search-web` and `search-workers` must be
healthy. If a dependency blocks the Web resource, use `aspire describe` for all resources first, then the
logs of the first non-healthy dependency rather than guessing from the Web symptom.

The connected path to observe is: B2B seed simulator event -> Service Bus emulator -> Search Workers
upsert/inbox -> `SearchDb` -> Search Web autocomplete. Obtain Search Web's **HTTP** endpoint from
`aspire describe search-web`, then query an expected B2B fixture through the actual optional
`headerType` and `searchTerm` parameters:

Pinned fixture — B2B `0.2.0-alpha.0.302`, source commit
`a1baf9a77daec9b7cb23ac69dcf6840d21fc503c`: artist ID `1`, `The Rockers`.

```powershell
$searchWebHttpUrl = '<HTTP endpoint shown for search-web>'.TrimEnd('/')
$expectedArtistId = 1
$expectedArtistName = 'The Rockers'
$searchTerm = [uri]::EscapeDataString($expectedArtistName)
$results = Invoke-RestMethod -Uri "$searchWebHttpUrl/api/Autocomplete?headerType=Artist&searchTerm=$searchTerm"
$match = $results | Where-Object {
  $_.PSObject.Properties['$type'].Value -eq 'artist' -and
  $_.id -eq $expectedArtistId -and
  $_.name -eq $expectedArtistName
}
if ($null -eq $match) { throw 'Pinned artist projection was not returned by autocomplete.' }
$match
```

The response contract is an array of objects with `$type`, `id`, and `name`; the Search contract declares
`$type` as the `HeaderType` enum and the TestClient reads it as the camel-case string `artist`, `venue`, or
`concert`. The E2E fixture derives its values dynamically from the same pinned `SeedCatalog` and requires
exact type, ID, and name matches for the first artist, first venue, and an observable posted future concert.
No successful local response has been observed for this documentation change.

### Current local constraints and data safety

- Qualification awaits a published platform pin containing the PostGIS mount fix. The current
  `0.2.0-alpha.0.14` pin is broken; the repair exists only in unpublished platform commit `2dda7db`. There
  is no local mount workaround and this setup makes no retained-store persistence claim.
- The native generated PostgreSQL password is designed to persist with AppHost user secrets. The effective
  password of an older retained store is unverified, so keep credentials and that store together as runtime
  evidence; do not recreate a database to make them appear to match.
- The Postgres volume name is derived from `AppContext.BaseDirectory`. A different checkout or build
  configuration can select a different store. Treat an anonymous retained database as a lesson artifact:
  never delete or recreate it. An authorised transition requires a backup, a verified restore, and explicit
  approval. Retention verification requires a non-seeded sentinel surviving container recreation and an
  observed database identity; simulator rows alone are replayable evidence, not persistence evidence.
- Simulator replay uses fresh envelope IDs and projection upserts. The emulator's broker data is not
  durable. Re-run the simulator to rebuild projections; do not mistake replay for stored state.
- The current B2B image and packages are the coherent `0.2.0-alpha.0.302` set. The known extra `Awaiting
  Show` concert 6 remains until a corrected simulator and `Seed.Contracts` package are published and the
  projection is reconciled; do not use that row as a success criterion.
- Standalone Search receives catalog events but no seeded Customer-origin rating events, so seeded ratings
  are absent. This is recorded in [`TECH_DEBT.md`](./TECH_DEBT.md).
- The on-demand infrastructure exercise belongs to the [Infra repository](https://github.com/Concertable/infra).
  Its unpublished `Feature/LocalAzureLearning` branch at `f331b58` contains the separate
  `labs/azure-local/verify.ps1` exercise and ARM records for only a resource group, VNet, and subnet. It
  does not provide cloud parity.

## Building the independent closure

The deployable closure consumes Concertable's shared platform and cross-service contracts as NuGet
`PackageReference`s from the private org feed `https://nuget.pkg.github.com/Concertable`. Restoring
them needs a GitHub [personal access token](https://github.com/settings/tokens) with the
**`read:packages`** scope, exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet restore Concertable.Search.slnx
dotnet build Concertable.Search.slnx --configuration Release --no-restore
dotnet publish api/src/Concertable.Search.Migrations/Concertable.Search.Migrations.csproj --configuration Release
dotnet pack api/src/Concertable.Search.Hosting/Concertable.Search.Hosting.csproj --configuration Release --output artifacts/packages -p:MinVerVersionOverride=0.0.0-local
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
manifest before removing its owned temporary directory and images. The Trivy cache volume is
reused across runs rather than removed — a cache destroyed each run is cold by construction:

```sh
pwsh ./scripts/verify-search-release-candidate.ps1
```

Pass `-KeepArtifacts -OutputPath <path-outside-the-repository>` to inspect the verified bundle locally. The
bundle records intended repositories and tags but contains no registry or NuGet publisher; verification cannot
push it. CI runs this combined rehearsal in addition to the normal build/test gate.

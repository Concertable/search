# Concertable.Search

The **Search** service of [Concertable](https://github.com/Concertable/concertable) — the search
*data service*: it maintains read-optimised projections of concerts, venues, and artists, built
purely from `*.Contracts` integration events published by the B2B and Customer services. It never
depends on another data service's runtime; when a standalone host lacks another service's events at
seed time, a `*.Seed.Simulator` replays them.

## Canonical source vs. this mirror

Development happens in the **monorepo** ([`Concertable/concertable`](https://github.com/Concertable/concertable)),
under `api/Concertable.Search/`. That folder is **automatically mirrored** to the read-only repo
[`Concertable/concertable-search`](https://github.com/Concertable/concertable-search) on every push
to `master`. **Don't open PRs against the mirror** — nothing flows back from it.

## Building standalone

The deployable closure consumes Concertable's shared platform and cross-service contracts as NuGet
`PackageReference`s from the private org feed `https://nuget.pkg.github.com/Concertable`. Restoring
them needs a GitHub [personal access token](https://github.com/settings/tokens) with the
**`read:packages`** scope, exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet build src/Concertable.Search.Web/Concertable.Search.Web.csproj
dotnet build src/Concertable.Search.Workers/Concertable.Search.Workers.csproj
```

Building the two host projects pulls the whole deployable closure. (In the monorepo's CI the same
variable is supplied by the workflow's `GITHUB_TOKEN`; standalone, you export your own PAT.)

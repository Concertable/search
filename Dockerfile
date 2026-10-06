# syntax=docker/dockerfile:1.27.1@sha256:4edf897a3ffa55b89f906fc8cc78afdb3f1834cc9c7083565e611a8a7d5fe99e

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:10.0@sha256:a365ce6a50b09176855d085c69da3fc1204a48432e36087e9a208f6e5860e235
ARG BUILD_VERSION=0.0.0-local

FROM ${DOTNET_SDK_IMAGE} AS source
WORKDIR /src
COPY . .

FROM source AS publish
ARG BUILD_VERSION
RUN --mount=type=secret,id=GITHUB_PACKAGES_TOKEN,required=true \
    GITHUB_PACKAGES_TOKEN="$(cat /run/secrets/GITHUB_PACKAGES_TOKEN)" && \
    export GITHUB_PACKAGES_TOKEN && \
    dotnet restore api/src/Concertable.Search.Web/Concertable.Search.Web.csproj && \
    dotnet restore api/src/Concertable.Search.Workers/Concertable.Search.Workers.csproj && \
    dotnet restore api/src/Concertable.Search.Migrations/Concertable.Search.Migrations.csproj
RUN dotnet publish api/src/Concertable.Search.Web/Concertable.Search.Web.csproj \
        --configuration Release --no-restore --output /out/web \
        -p:MinVerVersionOverride=${BUILD_VERSION} && \
    dotnet publish api/src/Concertable.Search.Workers/Concertable.Search.Workers.csproj \
        --configuration Release --no-restore --output /out/workers \
        -p:MinVerVersionOverride=${BUILD_VERSION} && \
    dotnet publish api/src/Concertable.Search.Migrations/Concertable.Search.Migrations.csproj \
        --configuration Release --no-restore --output /out/migrations \
        -p:MinVerVersionOverride=${BUILD_VERSION}

FROM ${DOTNET_ASPNET_IMAGE} AS search-web
ARG VCS_REF
ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/Concertable/search" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="Concertable Search Web" \
      org.opencontainers.image.description="Concertable public search API"
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /out/web .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Search.Web.dll"]

FROM ${DOTNET_ASPNET_IMAGE} AS search-workers
ARG VCS_REF
ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/Concertable/search" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="Concertable Search Workers" \
      org.opencontainers.image.description="Concertable search projection workers"
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /out/workers .
ENV DOTNET_EnableDiagnostics=0
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Search.Workers.dll"]

FROM ${DOTNET_ASPNET_IMAGE} AS search-migrations
ARG VCS_REF
ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/Concertable/search" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="Concertable Search Migrations" \
      org.opencontainers.image.description="One-shot Concertable Search database migration job"
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /out/migrations .
ENV DOTNET_EnableDiagnostics=0
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Search.Migrations.dll"]

# syntax=docker/dockerfile:1.10

ARG DOTNET_VERSION=10.0
ARG BUILD_VERSION=0.0.0-local

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS source
WORKDIR /src
COPY . .

FROM source AS publish
ARG BUILD_VERSION
RUN --mount=type=secret,id=GITHUB_PACKAGES_TOKEN,env=GITHUB_PACKAGES_TOKEN,required=true \
    dotnet restore src/Concertable.Search.Web/Concertable.Search.Web.csproj && \
    dotnet restore src/Concertable.Search.Workers/Concertable.Search.Workers.csproj && \
    dotnet restore src/Concertable.Search.Migrations/Concertable.Search.Migrations.csproj
RUN dotnet publish src/Concertable.Search.Web/Concertable.Search.Web.csproj \
        --configuration Release --no-restore --output /out/web \
        -p:MinVerVersionOverride=${BUILD_VERSION} && \
    dotnet publish src/Concertable.Search.Workers/Concertable.Search.Workers.csproj \
        --configuration Release --no-restore --output /out/workers \
        -p:MinVerVersionOverride=${BUILD_VERSION} && \
    dotnet publish src/Concertable.Search.Migrations/Concertable.Search.Migrations.csproj \
        --configuration Release --no-restore --output /out/migrations \
        -p:MinVerVersionOverride=${BUILD_VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS search-web
WORKDIR /app
COPY --from=publish /out/web .
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Search.Web.dll"]

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS search-workers
WORKDIR /app
COPY --from=publish /out/workers .
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Search.Workers.dll"]

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS search-migrations
WORKDIR /app
COPY --from=publish /out/migrations .
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Search.Migrations.dll"]

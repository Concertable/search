using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Contracts;
using Concertable.Search.Hosting;
using Concertable.Search.TestKit;
using Npgsql;

namespace Concertable.Search.E2ETests;

public sealed class SeedConvergenceTests
{
    private const string PostgresDataDirectory = "/var/lib/postgresql/data";
    private const string SentinelTable = "e2e_retention_sentinel";

    [Fact]
    public async Task IsolatedPostgresVolume_RecreatedAppHost_RetainsAuthAndSearchState()
    {
        using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var runId = Guid.NewGuid().ToString("N");
        var volume = new IsolatedPostgresVolume(runId);
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Concertable_Search_AppHost>(CreateAppHostArguments(runId), startupTimeout.Token);
        var postgresResourceName = ConfigureIsolatedPostgresVolume(builder, volume.Name);

        await volume.CreateAsync(startupTimeout.Token);
        try
        {
            string firstPostgresContainerId;
            await using (var first = await StartAsync(builder, startupTimeout.Token))
            {
                firstPostgresContainerId = await GetContainerIdAsync(first, postgresResourceName, startupTimeout.Token);
                await AssertReadyAndSeededAsync(first, builder, startupTimeout.Token);
                await WriteSentinelsAsync(first, runId, startupTimeout.Token);
            }

            await volume.AssertContainerRemovedAsync(firstPostgresContainerId, startupTimeout.Token);

            var restartedBuilder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Concertable_Search_AppHost>(CreateAppHostArguments(runId), startupTimeout.Token);
            var restartedPostgresResourceName = ConfigureIsolatedPostgresVolume(restartedBuilder, volume.Name);

            await using var restarted = await StartAsync(restartedBuilder, startupTimeout.Token);
            var restartedPostgresContainerId = await GetContainerIdAsync(
                restarted,
                restartedPostgresResourceName,
                startupTimeout.Token);
            Assert.NotEqual(firstPostgresContainerId, restartedPostgresContainerId);
            await AssertReadyAndSeededAsync(restarted, restartedBuilder, startupTimeout.Token);
            await AssertSentinelsRemainAsync(restarted, runId, startupTimeout.Token);
        }
        finally
        {
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await volume.RemoveIfUnusedAsync(cleanupTimeout.Token);
        }
    }

    private static string[] CreateAppHostArguments(string runId) =>
    [
        $"--Parameters:postgres-password=search-e2e-postgres-{runId}",
        $"--Parameters:auth-b2b-client-secret=search-e2e-b2b-{runId}",
        $"--Parameters:auth-customer-client-secret=search-e2e-customer-{runId}",
        $"--Parameters:auth-service-client-secret=search-e2e-service-{runId}"
    ];

    private static string ConfigureIsolatedPostgresVolume(
        IDistributedApplicationTestingBuilder builder,
        string volumeName)
    {
        var postgres = Assert.Single(builder.Resources.OfType<PostgresServerResource>());
        var mount = Assert.Single(
            postgres.Annotations.OfType<ContainerMountAnnotation>(),
            candidate => candidate.Target == PostgresDataDirectory);

        Assert.Equal(ContainerMountType.Volume, mount.Type);
        Assert.False(mount.IsReadOnly);
        Assert.False(string.IsNullOrWhiteSpace(mount.Source));
        Assert.DoesNotContain(
            postgres.Annotations.OfType<ContainerMountAnnotation>(),
            candidate => candidate.Target == "/var/lib/postgresql");

        postgres.Annotations.Remove(mount);
        builder.CreateResourceBuilder(postgres).WithVolume(volumeName, mount.Target, mount.IsReadOnly);
        return postgres.Name;
    }

    private static async Task<DistributedApplication> StartAsync(
        IDistributedApplicationTestingBuilder builder,
        CancellationToken cancellationToken)
    {
        var app = await builder.BuildAsync(cancellationToken);
        try
        {
            await app.StartAsync(cancellationToken);
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    private static async Task AssertReadyAndSeededAsync(
        DistributedApplication app,
        IDistributedApplicationTestingBuilder builder,
        CancellationToken cancellationToken)
    {
        var resources = builder.Resources.Select(resource => resource.Name).ToArray();
        await WaitForHealthyAsync(app, resources, AuthConstants.Resource, cancellationToken);
        await WaitForHealthyAsync(app, resources, SearchConstants.WorkersResource, cancellationToken);
        await WaitForHealthyAsync(app, resources, SearchConstants.WebResource, cancellationToken);
        await AssertExitedSuccessfullyAsync(app, AuthConstants.MigrationsResource, cancellationToken);
        await AssertExitedSuccessfullyAsync(app, SearchConstants.MigrationsResource, cancellationToken);
        await AssertExitedSuccessfullyAsync(app, B2BSeedingSimulator.Name, cancellationToken);
        await AssertAuthDiscoveryAsync(app, cancellationToken);

        using var httpClient = app.CreateHttpClient(SearchConstants.WebResource, "http");
        var search = new SearchTestClient(httpClient);
        var seed = new SeedCatalog(TimeProvider.System);
        var observableConcert = seed.Concerts.First(concert =>
            concert.DatePosted is not null && concert.Period.End > seed.Now);

        await AssertProjectionAsync(search, SearchProjectionType.Artist, seed.Artists[0].ArtistId, seed.Artists[0].Name, cancellationToken);
        await AssertProjectionAsync(search, SearchProjectionType.Venue, seed.Venues[0].VenueId, seed.Venues[0].Name, cancellationToken);
        await AssertProjectionAsync(search, SearchProjectionType.Concert, observableConcert.ConcertId, observableConcert.Name, cancellationToken);

        var searchConnectionString = await GetConnectionStringAsync(app, SearchConstants.Database, cancellationToken);
        await AssertWebUsesSearchDatabaseAsync(app, searchConnectionString, cancellationToken);
        await AssertDatabaseStateAsync(searchConnectionString, SearchConstants.Database, "search", requirePostGis: true, cancellationToken);
        await AssertDatabaseStateAsync(
            await GetConnectionStringAsync(app, AuthConstants.Database, cancellationToken),
            AuthConstants.Database,
            "auth",
            requirePostGis: false,
            cancellationToken);
    }

    private static async Task AssertExitedSuccessfullyAsync(
        DistributedApplication app,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var @event = await app.ResourceNotifications.WaitForResourceAsync(
            resourceName,
            candidate => candidate.Snapshot.State?.Text == KnownResourceStates.Exited,
            cancellationToken);

        Assert.Equal(0, @event.Snapshot.ExitCode);
    }

    private static async Task AssertAuthDiscoveryAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        using var authClient = app.CreateHttpClient(AuthConstants.Resource, "https");
        using var response = await authClient.GetAsync(".well-known/openid-configuration", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> GetContainerIdAsync(
        DistributedApplication app,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var @event = await app.ResourceNotifications.WaitForResourceAsync(
            resourceName,
            candidate => candidate.Snapshot.State?.Text == KnownResourceStates.Running
                && candidate.Snapshot.Properties.Any(property =>
                    property.Name == "container.id"
                    && property.Value is string containerId
                    && !string.IsNullOrWhiteSpace(containerId)),
            cancellationToken);

        return Assert.IsType<string>(
            @event.Snapshot.Properties.Single(property => property.Name == "container.id").Value);
    }

    private static async Task<string> GetConnectionStringAsync(
        DistributedApplication app,
        string resourceName,
        CancellationToken cancellationToken)
    {
        return await app.GetConnectionStringAsync(resourceName, cancellationToken)
            ?? throw new InvalidOperationException($"'{resourceName}' did not expose a connection string.");
    }

    private static async Task AssertWebUsesSearchDatabaseAsync(
        DistributedApplication app,
        string searchConnectionString,
        CancellationToken cancellationToken)
    {
        var @event = await app.ResourceNotifications.WaitForResourceAsync(
            SearchConstants.WebResource,
            candidate => candidate.Snapshot.EnvironmentVariables.Any(variable =>
                variable.Name == "ConnectionStrings__SearchDb" && variable.Value is string),
            cancellationToken);
        var configuredConnectionString = Assert.IsType<string>(
            @event.Snapshot.EnvironmentVariables.Single(variable =>
                variable.Name == "ConnectionStrings__SearchDb").Value);
        var configured = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        var actual = new NpgsqlConnectionStringBuilder(searchConnectionString);

        Assert.Equal(actual.Host, configured.Host);
        Assert.Equal(actual.Port, configured.Port);
        Assert.Equal(actual.Database, configured.Database);
    }

    private static async Task AssertDatabaseStateAsync(
        string connectionString,
        string expectedDatabase,
        string migrationSchema,
        bool requirePostGis,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        Assert.Equal(expectedDatabase, await ExecuteScalarAsync<string>(connection, "SELECT current_database()", cancellationToken));
        using var identifiers = new NpgsqlCommandBuilder();
        var history = $"{identifiers.QuoteIdentifier(migrationSchema)}.\"__EFMigrationsHistory\"";
        Assert.True(await ExecuteScalarAsync<long>(connection, $"SELECT COUNT(*) FROM {history}", cancellationToken) > 0);
        if (requirePostGis)
            Assert.False(string.IsNullOrWhiteSpace(await ExecuteScalarAsync<string>(connection, "SELECT PostGIS_Version()", cancellationToken)));
    }

    private static async Task WriteSentinelsAsync(DistributedApplication app, string runId, CancellationToken cancellationToken)
    {
        await WriteSentinelAsync(await GetConnectionStringAsync(app, SearchConstants.Database, cancellationToken), runId, cancellationToken);
        await WriteSentinelAsync(await GetConnectionStringAsync(app, AuthConstants.Database, cancellationToken), runId, cancellationToken);
    }

    private static async Task WriteSentinelAsync(string connectionString, string runId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var create = new NpgsqlCommand(
            $"CREATE TABLE IF NOT EXISTS {SentinelTable} (run_id text PRIMARY KEY)", connection);
        await create.ExecuteNonQueryAsync(cancellationToken);
        await using var insert = new NpgsqlCommand(
            $"INSERT INTO {SentinelTable} (run_id) VALUES (@runId) ON CONFLICT (run_id) DO NOTHING", connection);
        insert.Parameters.AddWithValue("runId", runId);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AssertSentinelsRemainAsync(DistributedApplication app, string runId, CancellationToken cancellationToken)
    {
        await AssertSentinelAsync(await GetConnectionStringAsync(app, SearchConstants.Database, cancellationToken), runId, cancellationToken);
        await AssertSentinelAsync(await GetConnectionStringAsync(app, AuthConstants.Database, cancellationToken), runId, cancellationToken);
    }

    private static async Task AssertSentinelAsync(string connectionString, string runId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM {SentinelTable} WHERE run_id = @runId", connection);
        command.Parameters.AddWithValue("runId", runId);
        Assert.Equal(1L, await ExecuteScalarAsync<long>(command, cancellationToken));
    }

    private static async Task<T> ExecuteScalarAsync<T>(NpgsqlConnection connection, string commandText, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(commandText, connection);
        return await ExecuteScalarAsync<T>(command, cancellationToken);
    }

    private static async Task<T> ExecuteScalarAsync<T>(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Assert.IsType<T>(result);
    }

    // Aspire reports only the resource that was waited on, so a dependency that died takes the blame with
    // it and the failure names search-web whatever actually broke. Re-throwing with every resource's state
    // is the difference between reading the cause and guessing at it.
    private static async Task WaitForHealthyAsync(
        DistributedApplication app,
        IReadOnlyCollection<string> resourceNames,
        string resourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(resourceName, cancellationToken);
        }
        catch (Exception exception)
        {
            var states = resourceNames.Select(name =>
                app.ResourceNotifications.TryGetCurrentState(name, out var current)
                    ? $"{name}={current.Snapshot.State?.Text ?? "unknown"}"
                        + (current.Snapshot.ExitCode is { } exitCode ? $"(exit {exitCode})" : string.Empty)
                    : $"{name}=unreported");

            throw new InvalidOperationException(
                $"'{resourceName}' never became healthy. Resource states: {string.Join(", ", states)}.",
                exception);
        }
    }

    private static async Task AssertProjectionAsync(
        SearchTestClient search,
        SearchProjectionType type,
        int id,
        string name,
        CancellationToken cancellationToken)
    {
        var projection = await search.WaitForProjectionAsync(
            type,
            id,
            name,
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(2),
            cancellationToken).ConfigureAwait(false);

        Assert.Equal(name, projection.Name);
    }
}

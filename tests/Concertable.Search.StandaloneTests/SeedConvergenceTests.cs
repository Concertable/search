using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Azure.Messaging.ServiceBus.Administration;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Contracts;
using Concertable.Search.Hosting;
using Concertable.Search.TestKit;
using Microsoft.Data.SqlClient;
using System.Globalization;
using Xunit.Abstractions;

namespace Concertable.Search.StandaloneTests;

public sealed class SeedConvergenceTests
{
    private static readonly IReadOnlyList<string> resourceNames =
    [
        "concertable-search-sql-data",
        SearchConstants.Database,
        "asb",
        AuthConstants.Resource,
        SearchConstants.MigrationsResource,
        SearchConstants.WebResource,
        SearchConstants.WorkersResource,
        B2BConstants.SeedingSimulatorResource
    ];

    private readonly ITestOutputHelper output;

    public SeedConvergenceTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task B2BSimulatorEventsRebuildEverySearchProjectionType()
    {
        using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Concertable_Search_AppHost>(startupTimeout.Token);

        await using var app = await builder.BuildAsync(startupTimeout.Token);

        try
        {
            await app.StartAsync(startupTimeout.Token);
            await app.ResourceNotifications.WaitForResourceHealthyAsync(
                SearchConstants.WebResource,
                startupTimeout.Token);
            await app.ResourceNotifications.WaitForResourceAsync(
                B2BConstants.SeedingSimulatorResource,
                KnownResourceStates.Exited,
                startupTimeout.Token);
            Assert.True(app.ResourceNotifications.TryGetCurrentState(
                B2BConstants.SeedingSimulatorResource,
                out var simulator));
            Assert.Equal(0, simulator.Snapshot.ExitCode);

            using var httpClient = app.CreateHttpClient(SearchConstants.WebResource);
            var search = new SearchTestClient(httpClient);
            var seed = new SeedCatalog(TimeProvider.System);

            await AssertProjectionAsync(
                search,
                SearchProjectionType.Artist,
                seed.Artists[0].ArtistId,
                seed.Artists[0].Name,
                startupTimeout.Token);
            await AssertProjectionAsync(
                search,
                SearchProjectionType.Venue,
                seed.Venues[0].VenueId,
                seed.Venues[0].Name,
                startupTimeout.Token);
            await AssertProjectionAsync(
                search,
                SearchProjectionType.Concert,
                seed.Concerts[0].ConcertId,
                seed.Concerts[0].Name,
                startupTimeout.Token);
        }
        catch
        {
            await TryWriteDiagnosticsAsync(
                "resource state",
                () =>
                {
                    WriteDiagnostics(app);
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            await TryWriteDiagnosticsAsync(
                "Service Bus",
                () => WriteServiceBusDiagnosticsAsync(app)).ConfigureAwait(false);
            await TryWriteDiagnosticsAsync(
                "Search database",
                () => WriteDatabaseDiagnosticsAsync(app)).ConfigureAwait(false);

            throw;
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

    private void WriteDiagnostics(DistributedApplication app)
    {
        foreach (var resourceName in resourceNames)
        {
            if (app.ResourceNotifications.TryGetCurrentState(resourceName, out var resource))
            {
                output.WriteLine(
                    "Resource {0}: state={1}, health={2}, exitCode={3}",
                    resourceName,
                    resource.Snapshot.State?.Text ?? "unknown",
                    resource.Snapshot.HealthStatus?.ToString() ?? "unknown",
                    resource.Snapshot.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "unknown");
            }

        }
    }

    private static async Task TryWriteDiagnosticsAsync(string name, Func<Task> writeDiagnostics)
    {
        try
        {
            await writeDiagnostics().ConfigureAwait(false);
        }
        catch (Exception diagnosticsException)
        {
            Console.Error.WriteLine($"Unable to collect {name} diagnostics: {diagnosticsException}");
        }
    }

    private async Task WriteServiceBusDiagnosticsAsync(DistributedApplication app)
    {
        var connectionString = await app.GetConnectionStringAsync("asb").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine("Service Bus diagnostics unavailable: connection string was not resolved.");
            return;
        }

        var managementEndpoint = app.GetEndpoint("asb", "emulatorhealth");
        var administrationEndpoint = new UriBuilder(managementEndpoint)
        {
            Scheme = "sb",
            Path = string.Empty
        }.Uri.GetLeftPart(UriPartial.Authority);
        var administrationConnectionString = string.Join(
            ';',
            connectionString.Split(';').Select(part =>
                part.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase)
                    ? $"Endpoint={administrationEndpoint}"
                    : part));
        using var diagnosticsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var administration = new ServiceBusAdministrationClient(administrationConnectionString);
        await foreach (var topic in administration
                           .GetTopicsAsync(diagnosticsTimeout.Token)
                           .ConfigureAwait(false))
        {
            await foreach (var subscription in administration
                               .GetSubscriptionsAsync(topic.Name, diagnosticsTimeout.Token)
                               .ConfigureAwait(false))
            {
                if (!string.Equals(
                        subscription.SubscriptionName,
                        SearchConstants.ServiceName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var response = await administration.GetSubscriptionRuntimePropertiesAsync(
                    topic.Name,
                    subscription.SubscriptionName,
                    diagnosticsTimeout.Token).ConfigureAwait(false);
                var properties = response.Value;
                output.WriteLine(
                    "Service Bus {0}/{1}: active={2}, deadLetter={3}, total={4}, transferDeadLetter={5}",
                    properties.TopicName,
                    properties.SubscriptionName,
                    properties.ActiveMessageCount,
                    properties.DeadLetterMessageCount,
                    properties.TotalMessageCount,
                    properties.TransferDeadLetterMessageCount);
            }
        }
    }

    private async Task WriteDatabaseDiagnosticsAsync(DistributedApplication app)
    {
        var connectionString = await app.GetConnectionStringAsync(SearchConstants.Database)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine("Search database diagnostics unavailable: connection string was not resolved.");
            return;
        }

        using var diagnosticsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(diagnosticsTimeout.Token).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT schema_name(tables.schema_id), tables.name, SUM(partitions.rows)
            FROM sys.tables AS tables
            JOIN sys.partitions AS partitions ON tables.object_id = partitions.object_id
            WHERE partitions.index_id IN (0, 1)
            GROUP BY tables.schema_id, tables.name
            ORDER BY schema_name(tables.schema_id), tables.name;
            """;
        await using var reader = await command.ExecuteReaderAsync(diagnosticsTimeout.Token)
            .ConfigureAwait(false);
        while (await reader.ReadAsync(diagnosticsTimeout.Token).ConfigureAwait(false))
        {
            output.WriteLine(
                "Search database {0}.{1}: rows={2}",
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt64(2));
        }
    }
}

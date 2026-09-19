extern alias SearchMigrations;

using Concertable.Search.Api;
using Concertable.Search.Infrastructure;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.Search.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Kernel;
using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using SearchMigrationJob = SearchMigrations::Concertable.Search.Migrations.SearchMigrationJob;

namespace Concertable.Search.IntegrationTests.Fixtures;

public sealed class ApiFixture : IAsyncLifetime
{
    private PostgresFixture postgresFixture = null!;
    private WebApplicationFactory<Program> factory = null!;
    private readonly XunitOutputAccessor outputAccessor = new();

    public void AttachOutput(ITestOutputHelper output) => outputAccessor.Output = output;
    public void DetachOutput() => outputAccessor.Output = null;

    public SeedState SeedState { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        postgresFixture = new PostgresFixture();
        await postgresFixture.InitializeAsync();
        await SearchMigrationJob.RunAsync(postgresFixture.ConnectionString);

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Integration);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SearchDb"] = postgresFixture.ConnectionString,
                });
                config.RelaxRateLimiting(RateLimitPolicies.All);
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddXunitLogging(outputAccessor);
                services.AddTestAuthentication();
                services.AddSearchProjectionTestSeeder();
                services.AddSearchProjectionHandlers();
            });
        });

        _ = factory.Services;
        await postgresFixture.InitializeRespawnerAsync(Schema.Owned);
    }

    public async Task ResetAsync()
    {
        await postgresFixture.ResetAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<ITestSeeder>().OrderBy(s => s.Order))
        {
            await seeder.MigrateAsync();
            await seeder.SeedAsync();
        }

        SeedState = scope.ServiceProvider.GetRequiredService<SeedState>();
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await postgresFixture.DisposeAsync();
    }

    public HttpClient CreateClient() => factory.CreateClient();

    public IServiceProvider Services => factory.Services;
    public string ConnectionString => postgresFixture.ConnectionString;

    public HttpClient CreateClient(Guid customerId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, customerId.ToString());
        return client;
    }
}

using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Data.Seeders;
using Concertable.Seed.Identity;
using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.Search.IntegrationTests.Fixtures;

public sealed class ApiFixture : IAsyncLifetime
{
    private SqlFixture sqlFixture = null!;
    private WebApplicationFactory<Program> factory = null!;
    private readonly XunitOutputAccessor outputAccessor = new();

    public void AttachOutput(ITestOutputHelper output) => outputAccessor.Output = output;
    public void DetachOutput() => outputAccessor.Output = null;

    public SeedState SeedState { get; } = new SeedState();

    public async Task InitializeAsync()
    {
        sqlFixture = new SqlFixture();
        await sqlFixture.InitializeAsync();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SearchDb"] = sqlFixture.ConnectionString,
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddLogging(b =>
                {
                    b.ClearProviders();
                    b.AddProvider(new XunitLoggerProvider(outputAccessor));
                    b.SetMinimumLevel(LogLevel.Information);
                });

                services.PostConfigure<AuthenticationOptions>(opts =>
                {
                    opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    opts.DefaultScheme = TestAuthHandler.SchemeName;
                });
                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });

        _ = factory.Services;
        await sqlFixture.InitializeRespawnerAsync();
    }

    public async Task ResetAsync()
    {
        await sqlFixture.ResetAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        await SearchTestSeeder.SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await sqlFixture.DisposeAsync();
    }

    public HttpClient CreateClient() => factory.CreateClient();

    public HttpClient CreateClient(Guid customerId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, customerId.ToString());
        return client;
    }
}

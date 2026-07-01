using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Testing;

namespace Concertable.Search.E2ETests.Helpers;

/// <summary>
/// Adds the real Search service to an E2E composition. The standalone AppHosts deliberately do not
/// run Search (data services never run their peers), but both surfaces' find pages are Search-backed
/// (B2B's /find/venue, Customer's find page), so the E2E suites that drive them run it themselves.
/// </summary>
public static class SearchServiceExtensions
{
    public static IDistributedApplicationTestingBuilder AddSearchService(
        this IDistributedApplicationTestingBuilder builder,
        string searchApiBaseUrl,
        string authBaseUrl)
    {
        var sql = builder.Resources.OfType<SqlServerServerResource>().Single();
        var asb = builder.Resources.OfType<AzureServiceBusResource>().Single();
        var auth = builder.Resources.OfType<ProjectResource>()
            .Single(r => r.Name == AppHostConstants.ResourceNames.Auth);

        var searchDb = builder.CreateResourceBuilder(sql).AddDatabase(AppHostConstants.Databases.Search);
        var authBuilder = builder.CreateResourceBuilder(auth);
        var asbBuilder = builder.CreateResourceBuilder(asb);

        builder.AddResource(new ProjectResource(AppHostConstants.ResourceNames.SearchWeb))
            .WithAnnotation(new SearchWebProject(builder.AppHostDirectory))
            .WithReference(searchDb)
            .WaitFor(searchDb)
            .WaitFor(authBuilder)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "E2E")
            .WithEnvironment("ASPNETCORE_URLS", searchApiBaseUrl)
            .WithEnvironment("Auth__Authority", authBaseUrl);

        builder.AddResource(new ProjectResource(AppHostConstants.ResourceNames.SearchWorkers))
            .WithAnnotation(new SearchWorkersProject(builder.AppHostDirectory))
            .WithReference(searchDb)
            .WaitFor(searchDb)
            .WithReference(asbBuilder)
            .WaitFor(asbBuilder)
            .WithEnvironment("DOTNET_ENVIRONMENT", "E2E");

        return builder;
    }

    private sealed class SearchWebProject : IProjectMetadata
    {
        private readonly string appHostDirectory;

        public SearchWebProject(string appHostDirectory)
        {
            this.appHostDirectory = appHostDirectory;
        }

        public string ProjectPath => Path.GetFullPath(Path.Combine(
            appHostDirectory, "..", "..", "..", "Concertable.Search", "src", "Concertable.Search.Web", "Concertable.Search.Web.csproj"));
    }

    private sealed class SearchWorkersProject : IProjectMetadata
    {
        private readonly string appHostDirectory;

        public SearchWorkersProject(string appHostDirectory)
        {
            this.appHostDirectory = appHostDirectory;
        }

        public string ProjectPath => Path.GetFullPath(Path.Combine(
            appHostDirectory, "..", "..", "..", "Concertable.Search", "src", "Concertable.Search.Workers", "Concertable.Search.Workers.csproj"));
    }
}

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Messaging.AzureServiceBus.Options;

public static class SearchAppHostExtensions
{
    public static IResourceBuilder<ProjectResource> AddSearchWeb<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<SqlServerDatabaseResource> searchDb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>(SearchConstants.WebResource)
                      .WithReference(auth)
                      .WaitFor(auth)
                      .WithReference(searchDb)
                      .WaitFor(searchDb)
                      .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));
    }

    public static IResourceBuilder<ProjectResource> AddSearchWorkers<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> searchDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        return builder.AddProject<TProject>(SearchConstants.WorkersResource)
                      .WithReference(searchDb)
                      .WaitFor(searchDb)
                      .WithReference(asb)
                      .WaitFor(asb)
                      .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, SearchConstants.ServiceName);
    }
}

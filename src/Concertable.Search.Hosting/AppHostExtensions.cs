using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Messaging.AzureServiceBus.Options;

namespace Concertable.Search.Hosting;

public static class AppHostExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ServiceContainerResource> AddSearchWeb(
            string image,
            string digest,
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
            IResourceBuilder<SqlServerDatabaseResource> searchDb)
        {
            return builder.AddContainerImage(SearchConstants.WebResource, image, digest)
                          .WithHttpEndpoint(targetPort: SearchConstants.ContainerPort, name: "https")
                          .WithReference(auth)
                          .WaitFor(auth)
                          .WithReference(searchDb)
                          .WaitFor(searchDb)
                          .WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));
        }

        public IResourceBuilder<ProjectResource> AddSearchWeb<TProject>(
            IResourceBuilder<IResourceWithServiceDiscovery> auth,
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

        public IResourceBuilder<ProjectResource> AddSearchWorkers<TProject>(
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

        public IResourceBuilder<ServiceContainerResource> AddSearchWorkers(
            string image,
            string digest,
            IResourceBuilder<SqlServerDatabaseResource> searchDb,
            IResourceBuilder<AzureServiceBusResource> asb)
        {
            return builder.AddContainerImage(SearchConstants.WorkersResource, image, digest)
                          .WithReference(searchDb)
                          .WaitFor(searchDb)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .WithEnvironment(AzureServiceBusOptions.ServiceNameEnvVar, SearchConstants.ServiceName);
        }
    }
}

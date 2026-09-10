using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.Search.Hosting;
using Xunit;

namespace Concertable.Search.StartupTests;

/// <summary>Covers the image overload of Search's own web service, which no AppHost in this repository
/// composes — every standalone AppHost runs its own service from source and only foreign services by
/// image. That gap is why the overload shipped declaring no endpoint at all while the project overload
/// gets Aspire's defaults for free: a consumer composing Search by image got a service nothing could
/// reach, and GetEndpoint("https") threw.</summary>
public sealed class ImageCompositionTests
{
    private const string Digest = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

    [Fact]
    public void AddSearchWeb_ByImage_DeclaresTheEndpointConsumersResolve()
    {
        var builder = DistributedApplication.CreateBuilder();
        var sql = builder.AddSqlServer("sql");
        var auth = builder.AddContainerImage(AuthConstants.Resource, "ghcr.io/concertable/auth", Digest)
                          .WithHttpEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");

        var web = builder.AddSearchWeb(
            "ghcr.io/concertable/search-web",
            Digest,
            auth,
            sql.AddDatabase(SearchConstants.Database));

        var endpoint = Assert.Single(
            web.Resource.Annotations.OfType<EndpointAnnotation>(),
            endpoint => endpoint.Name == "https");

        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal(SearchConstants.ContainerPort, endpoint.TargetPort);
    }
}

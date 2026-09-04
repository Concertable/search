using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.Testing.Architecture;
using Xunit;

namespace Concertable.Search.StartupTests;

public sealed class ResourceGraphTests
{
    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
    {
        var validBuilder = AppHost.CreateBuilder([]);
        AssertImageEndpoint(validBuilder, AuthConstants.Resource, "https", scheme: "https");
        AssertContainerRuntimeArgs(validBuilder, AuthConstants.Resource, "--user", "root");
        AssertUsesDeveloperCertificate(validBuilder, AuthConstants.Resource);
        using var app = validBuilder.Build();
        var builder = AppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    private static void AssertContainerRuntimeArgs(
        IDistributedApplicationBuilder builder,
        string resourceName,
        params object[] expected)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var args = new List<object>();
        foreach (var annotation in resource.Annotations.OfType<ContainerRuntimeArgsCallbackAnnotation>())
            annotation.Callback(new ContainerRuntimeArgsCallbackContext(args, CancellationToken.None))
                .GetAwaiter()
                .GetResult();

        Assert.Equal(expected, args);
    }

#pragma warning disable ASPIRECERTIFICATES001 // experimental API; asserts the temporary Auth image bridge
    private static void AssertUsesDeveloperCertificate(
        IDistributedApplicationBuilder builder,
        string resourceName)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var certificate = Assert.Single(resource.Annotations.OfType<HttpsCertificateAnnotation>());

        Assert.True(certificate.UseDeveloperCertificate);
    }
#pragma warning restore ASPIRECERTIFICATES001

    private static void AssertImageEndpoint(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string endpointName,
        string scheme)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var endpoint = Assert.Single(
            resource.Annotations.OfType<EndpointAnnotation>(),
            endpoint => endpoint.Name == endpointName);

        Assert.Equal(endpointName, endpoint.Name);
        Assert.Equal(scheme, endpoint.UriScheme);
        Assert.Equal(8080, endpoint.TargetPort);
    }
}

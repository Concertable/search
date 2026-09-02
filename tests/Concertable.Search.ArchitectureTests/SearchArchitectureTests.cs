using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Search.Hosting;
using Concertable.Search.Web;
using Concertable.Search.Workers;
using Concertable.Testing.Architecture;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Concertable.Search.ArchitectureTests;

public sealed class SearchArchitectureTests
{
    [Fact]
    public void Web_DevelopmentGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddSearchWebHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(Concertable.Search.Web.HostExtensions).Assembly]
        });
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.False(jwtOptions.RequireHttpsMetadata);
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSearchWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void Web_ProductionEnvironment_RequiresHttpsMetadata()
    {
        var arguments = CompositionTestArguments.Create();
        arguments[0] = "--environment=Production";
        var builder = WebApplication.CreateBuilder(arguments);
        builder.AddSearchWebHost();
        using var app = builder.Build();
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(jwtOptions.RequireHttpsMetadata);
    }

    [Fact]
    public void Workers_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        builder.AddSearchWorkerHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(Concertable.Search.Workers.HostExtensions).Assembly]
        });
        var invalidBuilder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSearchWorkerHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void AppHost_ProductionGraphAndStrictValidation_AreValid()
    {
        var validBuilder = SearchAppHost.CreateBuilder([]);
        AssertImage(validBuilder, AuthConstants.Resource,
            "8b7ba47efb319e6e1f1b5b86223d4075b9c8e09920933dae24fbf35f72851a63");
        AssertImage(validBuilder, B2BConstants.SeedingSimulatorResource,
            "a232e5f6a111e3c81479c53cc79d49c54a0bf18c4dcb75a2cbaa7bf3ec1a0957");
        AssertImageEndpoint(validBuilder, AuthConstants.Resource, "https");
        var migrations = Assert.IsType<ProjectResource>(validBuilder.Resources.Single(resource =>
            string.Equals(resource.Name, SearchConstants.MigrationsResource, StringComparison.Ordinal)));
        Assert.NotEmpty(migrations.Annotations.OfType<EnvironmentCallbackAnnotation>());
        AssertWaitsFor(
            validBuilder,
            SearchConstants.MigrationsResource,
            SearchConstants.Database,
            WaitType.WaitUntilHealthy);
        Assert.IsType<ProjectResource>(validBuilder.Resources.Single(resource =>
            string.Equals(resource.Name, SearchConstants.WebResource, StringComparison.Ordinal)));
        Assert.IsType<ProjectResource>(validBuilder.Resources.Single(resource =>
            string.Equals(resource.Name, SearchConstants.WorkersResource, StringComparison.Ordinal)));
        AssertWaitsFor(
            validBuilder,
            SearchConstants.WebResource,
            SearchConstants.MigrationsResource,
            WaitType.WaitForCompletion);
        AssertWaitsFor(
            validBuilder,
            SearchConstants.WorkersResource,
            SearchConstants.MigrationsResource,
            WaitType.WaitForCompletion);
        Assert.DoesNotContain(validBuilder.Resources,
            resource => string.Equals(resource.Name, B2BConstants.Database, StringComparison.Ordinal));
        using var app = validBuilder.Build();
        var builder = SearchAppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    private static void AssertImage(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string expectedSha256)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource =>
                string.Equals(resource.Name, resourceName, StringComparison.Ordinal)));
        var image = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());

        Assert.Equal(expectedSha256, image.SHA256);
    }

    private static void AssertImageEndpoint(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string endpointName)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource =>
                string.Equals(resource.Name, resourceName, StringComparison.Ordinal)));
        var endpoint = Assert.Single(
            resource.Annotations.OfType<EndpointAnnotation>(),
            endpoint => string.Equals(endpoint.Name, endpointName, StringComparison.Ordinal));

        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal(8080, endpoint.TargetPort);
    }

    private static void AssertWaitsFor(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string dependencyName,
        WaitType waitType)
    {
        var resource = builder.Resources.Single(candidate =>
            string.Equals(candidate.Name, resourceName, StringComparison.Ordinal));
        var wait = Assert.Single(
            resource.Annotations.OfType<WaitAnnotation>(),
            annotation => string.Equals(
                annotation.Resource.Name,
                dependencyName,
                StringComparison.Ordinal));

        Assert.Equal(waitType, wait.WaitType);
        if (waitType == WaitType.WaitForCompletion)
        {
            Assert.Equal(0, wait.ExitCode);
        }
    }
}

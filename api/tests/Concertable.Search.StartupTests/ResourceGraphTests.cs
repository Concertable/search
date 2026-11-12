using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Search.Hosting;
using Concertable.Testing.Architecture;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Concertable.Search.StartupTests;

public sealed class ResourceGraphTests
{
    private const string PostgresDataDirectory = "/var/lib/postgresql/data";

    [Fact]
    public async Task ProductionGraphAndStrictValidation_AreValid()
    {
        var validBuilder = AppHost.CreateBuilder([]);
        AssertImageEndpoint(validBuilder, AuthConstants.Resource, "https", scheme: "https");
        AssertContainerRuntimeArgs(validBuilder, AuthConstants.Resource, "--user", "root");
        AssertUsesDeveloperCertificate(validBuilder, AuthConstants.Resource);
        await AssertNoSpaClientsAsync(validBuilder);
        var migrations = Assert.IsType<ProjectResource>(validBuilder.Resources.Single(resource =>
            resource.Name == SearchConstants.MigrationsResource));
        Assert.NotEmpty(migrations.Annotations.OfType<EnvironmentCallbackAnnotation>());
        AssertWaitsFor(validBuilder, SearchConstants.MigrationsResource, SearchConstants.Database, WaitType.WaitUntilHealthy);
        Assert.IsType<ProjectResource>(validBuilder.Resources.Single(resource =>
            resource.Name == SearchConstants.WebResource));
        Assert.IsType<ProjectResource>(validBuilder.Resources.Single(resource =>
            resource.Name == SearchConstants.WorkersResource));
        AssertWaitsFor(validBuilder, SearchConstants.WebResource, SearchConstants.MigrationsResource, WaitType.WaitForCompletion);
        AssertWaitsFor(validBuilder, SearchConstants.WorkersResource, SearchConstants.MigrationsResource, WaitType.WaitForCompletion);
        AssertWaitsFor(validBuilder, B2BSeedingSimulator.Name, SearchConstants.WorkersResource, WaitType.WaitUntilHealthy);
        Assert.DoesNotContain(validBuilder.Resources, resource => resource.Name == B2BDatabase.Name);
        Assert.IsType<PostgresDatabaseResource>(validBuilder.Resources.Single(resource =>
            resource.Name == AuthConstants.Database));
        Assert.DoesNotContain(validBuilder.Resources, resource => resource.GetType().Name == "SqlServerServerResource");
        AssertWaitsFor(validBuilder, AuthConstants.MigrationsResource, AuthConstants.Database, WaitType.WaitUntilHealthy);
        AssertWaitsFor(validBuilder, AuthConstants.Resource, AuthConstants.MigrationsResource, WaitType.WaitForCompletion);
        using var app = validBuilder.Build();
        var builder = AppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Fact]
    public void PostgresDataVolume_IsWritableAndMountedAtTheDirectDataDirectory()
    {
        var builder = AppHost.CreateBuilder([]);
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
    }

    [Fact]
    public async Task AuthClientSecrets_GeneratedDefaults_AreSecretAndReachAuth()
    {
        var builder = AppHost.CreateBuilder(["--AppHost:Operation=publish"]);

        await AssertGeneratedSecretAsync(builder, "auth-b2b-client-secret");
        await AssertGeneratedSecretAsync(builder, "auth-customer-client-secret");
        await AssertGeneratedSecretAsync(builder, "auth-service-client-secret");

        var environment = await GetAuthEnvironmentAsync(builder);

        Assert.Equal("{auth-b2b-client-secret.value}", environment["ServiceAuth__B2BClientSecret"]);
        Assert.Equal("{auth-customer-client-secret.value}", environment["ServiceAuth__CustomerClientSecret"]);
        Assert.Equal("{auth-service-client-secret.value}", environment["ServiceAuth__AuthClientSecret"]);
    }

    [Fact]
    public async Task AuthClientSecrets_ParametersAreExplicitlyConfigured_UsesParameterValues()
    {
        var builder = AppHost.CreateBuilder(
        [
            "--AppHost:Operation=publish",
            "--Parameters:auth-b2b-client-secret=synthetic-b2b-client-secret",
            "--Parameters:auth-customer-client-secret=synthetic-customer-client-secret",
            "--Parameters:auth-service-client-secret=synthetic-service-client-secret"
        ]);

        Assert.Equal("synthetic-b2b-client-secret", await GetParameter(builder, "auth-b2b-client-secret").GetValueAsync(CancellationToken.None));
        Assert.Equal("synthetic-customer-client-secret", await GetParameter(builder, "auth-customer-client-secret").GetValueAsync(CancellationToken.None));
        Assert.Equal("synthetic-service-client-secret", await GetParameter(builder, "auth-service-client-secret").GetValueAsync(CancellationToken.None));

        var environment = await GetAuthEnvironmentAsync(builder);

        Assert.Equal("{auth-b2b-client-secret.value}", environment["ServiceAuth__B2BClientSecret"]);
        Assert.Equal("{auth-customer-client-secret.value}", environment["ServiceAuth__CustomerClientSecret"]);
        Assert.Equal("{auth-service-client-secret.value}", environment["ServiceAuth__AuthClientSecret"]);
    }

    private static void AssertWaitsFor(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string dependencyName,
        WaitType waitType)
    {
        var resource = builder.Resources.Single(candidate => candidate.Name == resourceName);
        var wait = Assert.Single(
            resource.Annotations.OfType<WaitAnnotation>(),
            annotation => annotation.Resource.Name == dependencyName);

        Assert.Equal(waitType, wait.WaitType);
        if (waitType == WaitType.WaitForCompletion)
        {
            Assert.Equal(0, wait.ExitCode);
        }
    }

    private static async Task AssertNoSpaClientsAsync(IDistributedApplicationBuilder builder)
    {
        var environment = await GetAuthEnvironmentAsync(builder);

        Assert.Equal("true", environment["Auth__SpaClients__RestrictToEnabledClients"]);
        Assert.DoesNotContain(
            environment.Keys,
            key => key.StartsWith("Auth__SpaClients__EnabledClients__", StringComparison.Ordinal));
        Assert.DoesNotContain(
            environment.Keys,
            key => key.StartsWith("Auth__SpaClients__", StringComparison.Ordinal)
                && key != "Auth__SpaClients__RestrictToEnabledClients");
    }

    private static async Task AssertGeneratedSecretAsync(IDistributedApplicationBuilder builder, string parameterName)
    {
        var parameter = GetParameter(builder, parameterName);
        var generatedDefault = Assert.IsType<GenerateParameterDefault>(parameter.Default);

        Assert.True(parameter.Secret);
        Assert.Equal(32, generatedDefault.MinLength);
        Assert.True((await parameter.GetValueAsync(CancellationToken.None)).Length >= generatedDefault.MinLength);
    }

    private static ParameterResource GetParameter(IDistributedApplicationBuilder builder, string parameterName)
    {
        return Assert.IsType<ParameterResource>(
            builder.Resources.Single(resource => resource.Name == parameterName));
    }

    private static async Task<Dictionary<string, string>> GetAuthEnvironmentAsync(IDistributedApplicationBuilder builder)
    {
        var auth = Assert.IsAssignableFrom<IResourceWithEnvironment>(
            builder.Resources.Single(resource => resource.Name == AuthConstants.Resource));
        var configuration = await ExecutionConfigurationBuilder.Create(auth)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
                NullLogger.Instance,
                CancellationToken.None);

        return configuration.EnvironmentVariables.ToDictionary();
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

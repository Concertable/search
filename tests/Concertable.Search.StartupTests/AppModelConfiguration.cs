using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Concertable.Search.StartupTests;

/// <summary>Resolves the configuration one AppHost resource actually receives, as host command-line
/// arguments. A host fed from here is fed what the app model supplies it, so a key the graph stops
/// supplying fails this tier rather than surfacing as an unrelated timeout three layers away in E2E.
/// Publish mode is deliberate: it resolves every reference without a running DCP, and the assertion is
/// that a required key is PRESENT, not that its value points anywhere live.</summary>
internal static class AppModelConfiguration
{
    /// <summary>The only keys a host may require that the app model legitimately does not carry: secrets the
    /// AppHosts forward through AddSecrets / WithOptionalEnvironment ONLY when already configured, so they are
    /// absent on a developer machine without user secrets and injected by CI. Keep this list to secrets. A
    /// topology or wiring key added here would hide exactly the defect this tier exists to catch.</summary>
    public static string[] Secrets { get; } =
    [
        "--ServiceAuth:AuthClientSecret=startup-auth-secret",
        "--ServiceAuth:B2BClientSecret=startup-b2b-secret",
        "--ServiceAuth:CustomerClientSecret=startup-customer-secret",
        "--ServiceAuth:ClientSecret=startup-client-secret",
        "--Stripe:SecretKey=sk_test_startup",
        "--Stripe:WebhookSecret=whsec_startup",
        "--ExternalServices:UseRealStripe=false"
    ];

    public static async Task<string[]> ArgumentsForAsync(
        IDistributedApplicationBuilder appModel,
        string resourceName,
        params string[] environmental)
    {
        var resource = (IResourceWithEnvironment)appModel.Resources.Single(
            candidate => candidate.Name == resourceName);
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var resolved = await ExecutionConfigurationBuilder.Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(executionContext, NullLogger.Instance, CancellationToken.None);

        return
        [
            .. environmental,
            .. resolved.EnvironmentVariables
                .Where(variable => variable.Value is not null)
                .Select(variable => $"--{variable.Key.Replace("__", ":")}={variable.Value}")
        ];
    }
}

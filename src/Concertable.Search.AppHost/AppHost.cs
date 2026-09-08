using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:06a295ad6fa01a223000682b0f6efbfba2d5436a8fb2ffaa2d2399526ff3ae69";
    private const string B2BSeedingSimulatorImage = "ghcr.io/concertable/b2b-seeding-simulator";
    private const string B2BSeedingSimulatorDigest = "sha256:a232e5f6a111e3c81479c53cc79d49c54a0bf18c4dcb75a2cbaa7bf3ec1a0957";

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var sql = builder.AddSqlServerContainer("concertable-search-sql-data");
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var searchDb = sql.AddDatabase(SearchConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology().AddSearchTopology().AddAuthTopology().RunAsEmulator();
        var auth = builder.AddAuth(AuthImage, AuthDigest, authDb, asb)
                          .WithContainerRuntimeArgs("--user", "root")
                          .WithHttpsEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");
        auth.WithSpaClients([]);
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb);
        builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb);
        builder.AddB2BSeedingSimulator(B2BSeedingSimulatorImage, B2BSeedingSimulatorDigest, asb);
        return builder;
    }
}

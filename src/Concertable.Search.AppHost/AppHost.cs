using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:8b7ba47efb319e6e1f1b5b86223d4075b9c8e09920933dae24fbf35f72851a63";
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
                          .WithHttpEndpoint(targetPort: 8080, name: "https")
                          .WithEnvironment("ASPNETCORE_URLS", "http://+:8080");
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb);
        builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb);
        builder.AddB2BSeedingSimulator(B2BSeedingSimulatorImage, B2BSeedingSimulatorDigest, asb);
        return builder;
    }
}

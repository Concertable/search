using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:06a295ad6fa01a223000682b0f6efbfba2d5436a8fb2ffaa2d2399526ff3ae69";
    private const string B2BSeedingSimulatorImage = "ghcr.io/concertable/b2b-seeding-simulator";
    private const string B2BSeedingSimulatorDigest = "sha256:240d9569035152fd7e63695bcb0913ca5bad85fef902d9669c7e3bc288d2e244";

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var sql = builder.AddSqlServer("sql").WithDataVolume("concertable-search-sql-data");
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var postgres = builder.AddPostgresContainer("concertable-search-postgres-data").WithPostGis();
        var searchDb = postgres.AddDatabase(SearchConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology()
           .AddSearchTopology()
           .AddAuthTopology()
           .Publish<PayoutOwnerRegisteredEvent>()
           .RunAsEmulator();
        var auth = builder.AddAuth(AuthImage, AuthDigest, authDb, asb)
                          .WithContainerRuntimeArgs("--user", "root")
                          .WithHttpsEndpoint(targetPort: AuthConstants.ContainerPort, name: "https");
        auth.WithSpaClients([]);
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        var migrations = builder.AddSearchMigrations<Projects.Concertable_Search_Migrations>(searchDb);
        builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb)
               .WaitForCompletion(migrations);
        var workers = builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb)
                             .WaitForCompletion(migrations);
        builder.AddB2BSeedingSimulator(B2BSeedingSimulatorImage, B2BSeedingSimulatorDigest, asb)
               .WaitFor(workers);
        return builder;
    }
}

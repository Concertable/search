using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Search.Hosting;

public static class AppHost
{
    private const string AuthImage = "ghcr.io/concertable/auth";
    private const string AuthDigest = "sha256:cbd7c429da9d9dd2cc674177760690c53d1414e8057e368eefc3631dfcb62be6";
    private const string AuthMigrationsImage = "ghcr.io/concertable/auth-migrations";
    private const string AuthMigrationsDigest = "sha256:090b1bb80dc7b708508a03883cdfb8e8805b36918589e6d14f2f350cc61c5dcb";
    private const string B2BSeedingSimulatorImage = "ghcr.io/concertable/b2b-seeding-simulator";
    private const string B2BSeedingSimulatorDigest = "sha256:240d9569035152fd7e63695bcb0913ca5bad85fef902d9669c7e3bc288d2e244";

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var postgres = builder.AddPostgresContainer("concertable-search-postgres-data").WithPostGis();
        var searchDb = postgres.AddDatabase(SearchConstants.Database);
        var authDb = postgres.AddDatabase(AuthConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology()
           .AddSearchTopology()
           .AddAuthTopology()
           .Publish<PayoutOwnerRegisteredEvent>()
           .RunAsEmulator();
        var authMigrations = builder.AddAuthMigrations(AuthMigrationsImage, AuthMigrationsDigest, authDb);
        var auth = builder.AddAuth(AuthImage, AuthDigest, authDb, authMigrations, asb)
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

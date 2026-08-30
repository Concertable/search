using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Search.Hosting;

public static class SearchAppHost
{
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var sql = builder.AddSqlServerContainer("concertable-search-sql-data");
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var searchDb = sql.AddDatabase(SearchConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology().AddSearchTopology().AddAuthTopology().RunAsEmulator();
        var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, asb);
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb);
        builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb);
        builder.AddB2BSeedingSimulator<Projects.Concertable_B2B_Seed_Simulator>(asb);
        return builder;
    }
}

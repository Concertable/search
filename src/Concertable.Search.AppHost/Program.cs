var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServerContainer("concertable-search-sql-data");
var authDb = sql.AddDatabase(AppHostConstants.Databases.Auth);
var b2bDb = sql.AddDatabase(AppHostConstants.Databases.B2B);
var searchDb = sql.AddDatabase(AppHostConstants.Databases.Search);

var asb = builder.AddServiceBus();
asb.Topology().AddSearchTopology();

var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");

var searchWeb = builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb);
builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb);

// Search projects B2B catalog events; replay them standalone via the B2B seed simulator.
// Customer-origin rating events have no simulator yet (see api/Concertable.Search/TECH_DEBT.md).
builder.AddB2BSeedingSimulator<Projects.Concertable_B2B_Seed_Simulator>(asb);

builder.Build().Run();

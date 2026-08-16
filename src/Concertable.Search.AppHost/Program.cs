using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.Search.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServerContainer("concertable-search-sql-data");
var authDb = sql.AddDatabase(AuthConstants.Database);
var b2bDb = sql.AddDatabase(B2BConstants.Database);
var searchDb = sql.AddDatabase(SearchConstants.Database);

var asb = builder.AddServiceBus();
asb.Topology().AddSearchTopology().AddAuthTopology().RunAsEmulator();

var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");

var searchWeb = builder.AddSearchWeb<Projects.Concertable_Search_Web>(auth, searchDb);
builder.AddSearchWorkers<Projects.Concertable_Search_Workers>(searchDb, asb);

// Search projects B2B catalog events; replay them standalone via the B2B seed simulator.
// Customer-origin rating events have no simulator yet (see api/Concertable.Search/TECH_DEBT.md).
builder.AddB2BSeedingSimulator<Projects.Concertable_B2B_Seed_Simulator>(asb);

builder.Build().Run();

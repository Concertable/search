using Concertable.Search.Migrations;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SearchDb")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings__SearchDb' is required for the Search migration job.");

await SearchMigrationJob.RunAsync(connectionString).ConfigureAwait(false);

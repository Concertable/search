using Concertable.Search.Migrations;
using Concertable.Testing.Integration;
using Npgsql;

namespace Concertable.Search.IntegrationTests;

public sealed class MigrationJobTests
{
    [Fact]
    public async Task RunAsync_CreatesOwnedAndInboxTables_AndCanRunAgain()
    {
        var postgres = new PostgresFixture();
        await postgres.InitializeAsync();
        try
        {
            await SearchMigrationJob.RunAsync(postgres.ConnectionString);
            await SearchMigrationJob.RunAsync(postgres.ConnectionString);

            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT table_schema || '.' || table_name
                FROM information_schema.tables
                WHERE table_name LIKE '__EFMigrationsHistory%'
                ORDER BY table_schema, table_name
                """;
            await using var reader = await command.ExecuteReaderAsync();
            var histories = new List<string>();
            while (await reader.ReadAsync())
                histories.Add(reader.GetString(0));

            Assert.Equal(
            [
                "messaging.__EFMigrationsHistory_Inbox",
                "search.__EFMigrationsHistory",
            ],
            histories);
        }
        finally
        {
            await postgres.DisposeAsync();
        }
    }
}

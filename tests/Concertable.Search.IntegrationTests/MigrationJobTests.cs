using Concertable.Search.Migrations;
using Concertable.Testing.Integration;
using Microsoft.Data.SqlClient;

namespace Concertable.Search.IntegrationTests;

public sealed class MigrationJobTests : IClassFixture<SqlFixture>
{
    private readonly SqlFixture sql;

    public MigrationJobTests(SqlFixture sql)
    {
        this.sql = sql;
    }

    [Fact]
    public async Task RunAsync_CreatesOwnedAndInboxTables_AndCanRunAgain()
    {
        await SearchMigrationJob.RunAsync(sql.ConnectionString).ConfigureAwait(true);
        await SearchMigrationJob.RunAsync(sql.ConnectionString).ConfigureAwait(true);

        var connection = new SqlConnection(sql.ConnectionString);
        await using (connection.ConfigureAwait(true))
        {
            await connection.OpenAsync().ConfigureAwait(true);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(true))
            {
                command.CommandText = """
                    SELECT COUNT(*)
                    FROM sys.tables AS tables
                    INNER JOIN sys.schemas AS schemas ON schemas.schema_id = tables.schema_id
                    WHERE (schemas.name = 'search' AND tables.name = 'Artists')
                       OR (schemas.name = 'messaging' AND tables.name = 'Inbox');
                    """;

                Assert.Equal(
                    2,
                    (int)(await command.ExecuteScalarAsync().ConfigureAwait(true))!);
            }
        }
    }
}

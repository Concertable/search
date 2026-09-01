using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Migrations;

public static class SearchMigrationJob
{
    public static async Task RunAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var searchOptions = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseNetTopologySuite())
            .Options;
        var searchContext = new SearchDbContext(
            searchOptions,
            new SearchConfigurationProvider());
        await using (searchContext.ConfigureAwait(false))
        {
            await searchContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        var inboxOptions = new DbContextOptionsBuilder<InboxDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var inboxContext = new InboxDbContext(inboxOptions);
        await using (inboxContext.ConfigureAwait(false))
        {
            await inboxContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

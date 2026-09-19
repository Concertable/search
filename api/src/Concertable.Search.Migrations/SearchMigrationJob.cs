using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Search.Infrastructure;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.Search.Migrations;

internal static class SearchMigrationJob
{
    public static async Task RunAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var searchOptions = new DbContextOptionsBuilder<SearchDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema.Name)
                    .UseNetTopologySuite())
            .Options;
        var searchContext = new SearchDbContext(
            searchOptions,
            Options.Create(new OutboxOptions()),
            new SearchConfigurationProvider());
        await using (searchContext.ConfigureAwait(false))
        {
            await MigrateAsync(searchContext, cancellationToken).ConfigureAwait(false);
        }

        var inboxOptions = new DbContextOptionsBuilder<InboxDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Inbox", Schema.Messaging))
            .Options;
        var inboxContext = new InboxDbContext(inboxOptions);
        await using (inboxContext.ConfigureAwait(false))
        {
            await MigrateAsync(inboxContext, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task MigrateAsync(DbContext context, CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
            throw new InvalidOperationException($"Migrations remain pending for {context.GetType().Name}.");
    }
}

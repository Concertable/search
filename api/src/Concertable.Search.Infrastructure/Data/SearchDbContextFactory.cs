using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Extensions.Options;

namespace Concertable.Search.Infrastructure.Data;

internal sealed class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
{
    public SearchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseNpgsql(
                DesignTimeConfiguration.ConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema.Name)
                    .UseNetTopologySuite())
            .Options;
        return new SearchDbContext(
            options,
            Options.Create(new OutboxOptions()),
            new SearchConfigurationProvider());
    }
}

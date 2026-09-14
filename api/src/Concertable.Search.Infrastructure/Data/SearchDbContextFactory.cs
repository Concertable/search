using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.Search.Infrastructure.Data;

internal sealed class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
{
    public SearchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlServer(DesignTimeConfiguration.ConnectionString(), sql => sql.UseNetTopologySuite())
            .Options;
        return new SearchDbContext(options, new SearchConfigurationProvider());
    }
}

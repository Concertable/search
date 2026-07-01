using Concertable.Search.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Data.Seeders;

internal sealed class SearchProjectionTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly SearchDbContext context;
    private readonly SeedState seedData;

    public SearchProjectionTestSeeder(SearchDbContext context, SeedState seedData)
    {
        this.context = context;
        this.seedData = seedData;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Set<ArtistReadModel>().SeedIfEmptyAsync(async () =>
        {
            context.Set<ArtistReadModel>().AddRange(seedData.Artists);
            context.Set<VenueReadModel>().AddRange(seedData.Venues);
            context.Set<ConcertReadModel>().AddRange(seedData.Concerts);
            await context.SaveChangesAsync(ct);
        });
    }
}

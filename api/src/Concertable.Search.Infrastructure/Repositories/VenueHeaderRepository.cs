using Concertable.Contracts;
using Concertable.DataAccess;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class VenueHeaderRepository : IVenueHeaderRepository
{
    private readonly ISearchDbContext context;
    private readonly IVenueSearchQueryBuilder queryBuilder;

    public VenueHeaderRepository(
        ISearchDbContext context,
        IVenueSearchQueryBuilder queryBuilder)
    {
        this.context = context;
        this.queryBuilder = queryBuilder;
    }

    public async Task<IPagination<VenueHeader>> SearchAsync(SearchParams searchParams)
    {
        return await this.queryBuilder
            .Build(this.context.Venues.AsNoTracking(), searchParams)
            .ToHeaderDtos(context.VenueRatingProjections.AsNoTracking())
            .ToPaginationAsync(searchParams);
    }

    public async Task<IReadOnlyList<VenueHeader>> GetByAmountAsync(int amount) =>
        await context.Venues.OrderBy(v => v.Id)
            .ToHeaderDtos(context.VenueRatingProjections.AsNoTracking())
            .Take(amount)
            .ToListAsync();
}

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
    private readonly IVenueSearchSpecification searchSpecification;
    private readonly IGeometrySpecification<VenueReadModel> geometrySpecification;
    private readonly ISortSpecification<VenueReadModel> sortSpecification;

    public VenueHeaderRepository(
        ISearchDbContext context,
        IVenueSearchSpecification searchSpecification,
        IGeometrySpecification<VenueReadModel> geometrySpecification,
        ISortSpecification<VenueReadModel> sortSpecification)
    {
        this.context = context;
        this.searchSpecification = searchSpecification;
        this.geometrySpecification = geometrySpecification;
        this.sortSpecification = sortSpecification;
    }

    public async Task<IPagination<VenueHeader>> SearchAsync(SearchParams searchParams)
    {
        var query = searchSpecification.Apply(context.Venues.AsNoTracking(), searchParams);
        query = geometrySpecification.Apply(query, searchParams);
        query = sortSpecification.Apply(query, searchParams.Sort);
        return await query
            .ToHeaderDtos(context.VenueRatingProjections.AsNoTracking())
            .ToPaginationAsync(searchParams);
    }

    public async Task<IEnumerable<VenueHeader>> GetByAmountAsync(int amount) =>
        await context.Venues.OrderBy(v => v.Id)
            .ToHeaderDtos(context.VenueRatingProjections.AsNoTracking())
            .Take(amount)
            .ToListAsync();
}

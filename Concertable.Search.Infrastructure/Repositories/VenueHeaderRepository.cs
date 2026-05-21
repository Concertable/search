using Concertable.DataAccess;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal class VenueHeaderRepository : IVenueHeaderRepository
{
    private readonly ISearchDbContext context;
    private readonly IVenueSearchSpecification searchSpecification;
    private readonly IGeometrySpecification<VenueSearchModel> geometrySpecification;
    private readonly ISortSpecification<VenueHeaderDto> sortSpecification;

    public VenueHeaderRepository(
        ISearchDbContext context,
        IVenueSearchSpecification searchSpecification,
        IGeometrySpecification<VenueSearchModel> geometrySpecification,
        ISortSpecification<VenueHeaderDto> sortSpecification)
    {
        this.context = context;
        this.searchSpecification = searchSpecification;
        this.geometrySpecification = geometrySpecification;
        this.sortSpecification = sortSpecification;
    }

    public async Task<IPagination<VenueHeaderDto>> SearchAsync(SearchParams searchParams)
    {
        var query = searchSpecification.Apply(context.Venues.AsNoTracking(), searchParams);
        query = geometrySpecification.Apply(query, searchParams);
        var dtos = sortSpecification.Apply(
            query.ToHeaderDtos(context.VenueRatingProjections.AsNoTracking()),
            searchParams);
        return await dtos.ToPaginationAsync(searchParams);
    }

    public async Task<IEnumerable<VenueHeaderDto>> GetByAmountAsync(int amount) =>
        await context.Venues            .OrderBy(v => v.Id)
            .ToHeaderDtos(context.VenueRatingProjections.AsNoTracking())
            .Take(amount)
            .ToListAsync();
}

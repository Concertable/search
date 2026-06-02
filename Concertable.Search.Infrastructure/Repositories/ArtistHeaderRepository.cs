using Concertable.Contracts;
using Concertable.DataAccess;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ArtistHeaderRepository : IArtistHeaderRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchSpecification searchSpecification;
    private readonly IGeometrySpecification<ArtistReadModel> geometrySpecification;
    private readonly ISortSpecification<ArtistHeader> sortSpecification;

    public ArtistHeaderRepository(
        ISearchDbContext context,
        IArtistSearchSpecification searchSpecification,
        IGeometrySpecification<ArtistReadModel> geometrySpecification,
        ISortSpecification<ArtistHeader> sortSpecification)
    {
        this.context = context;
        this.searchSpecification = searchSpecification;
        this.geometrySpecification = geometrySpecification;
        this.sortSpecification = sortSpecification;
    }

    public async Task<IPagination<ArtistHeader>> SearchAsync(SearchParams searchParams)
    {
        var query = searchSpecification.Apply(context.Artists.AsNoTracking(), searchParams);
        query = geometrySpecification.Apply(query, searchParams);
        var dtos = sortSpecification.Apply(
            query.ToHeaderDtos(context.ArtistRatingProjections.AsNoTracking()),
            searchParams);
        return await dtos.ToPaginationAsync(searchParams);
    }

    public async Task<IEnumerable<ArtistHeader>> GetByAmountAsync(int amount) =>
        await context.Artists.OrderBy(a => a.Id)
            .ToHeaderDtos(context.ArtistRatingProjections.AsNoTracking())
            .Take(amount)
            .ToListAsync();
}

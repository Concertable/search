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
    private readonly IArtistSearchQuery searchQuery;

    public ArtistHeaderRepository(
        ISearchDbContext context,
        IArtistSearchQuery searchQuery)
    {
        this.context = context;
        this.searchQuery = searchQuery;
    }

    public async Task<IPagination<ArtistHeader>> SearchAsync(SearchParams searchParams)
    {
        return await this.searchQuery
            .Apply(this.context.Artists.AsNoTracking(), searchParams)
            .ToHeaderDtos(context.ArtistRatingProjections.AsNoTracking())
            .ToPaginationAsync(searchParams);
    }

    public async Task<IReadOnlyList<ArtistHeader>> GetByAmountAsync(int amount) =>
        await context.Artists.OrderBy(a => a.Id)
            .ToHeaderDtos(context.ArtistRatingProjections.AsNoTracking())
            .Take(amount)
            .ToListAsync();
}

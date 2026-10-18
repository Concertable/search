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
    private readonly IArtistSearchQueryBuilder queryBuilder;

    public ArtistHeaderRepository(
        ISearchDbContext context,
        IArtistSearchQueryBuilder queryBuilder)
    {
        this.context = context;
        this.queryBuilder = queryBuilder;
    }

    public async Task<IPagination<ArtistHeader>> SearchAsync(SearchParams searchParams)
    {
        return await this.queryBuilder
            .Build(this.context.Artists.AsNoTracking(), searchParams)
            .ToHeaderDtos(context.ArtistRatingProjections.AsNoTracking())
            .ToPaginationAsync(searchParams);
    }

    public async Task<IReadOnlyList<ArtistHeader>> GetByAmountAsync(int amount) =>
        await context.Artists.OrderBy(a => a.Id)
            .ToHeaderDtos(context.ArtistRatingProjections.AsNoTracking())
            .Take(amount)
            .ToListAsync();
}

using Concertable.DataAccess;
using Concertable.Search.Domain.Models;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.Search.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal class ConcertHeaderRepository : IConcertHeaderRepository
{
    private readonly ISearchDbContext context;
    private readonly IConcertSearchSpecification searchSpecification;
    private readonly IGeometrySpecification<ConcertReadModel> geometrySpecification;
    private readonly ISortSpecification<ConcertHeaderDto> sortSpecification;
    private readonly TimeProvider timeProvider;

    public ConcertHeaderRepository(
        ISearchDbContext context,
        IConcertSearchSpecification searchSpecification,
        IGeometrySpecification<ConcertReadModel> geometrySpecification,
        ISortSpecification<ConcertHeaderDto> sortSpecification,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.searchSpecification = searchSpecification;
        this.geometrySpecification = geometrySpecification;
        this.sortSpecification = sortSpecification;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<ConcertHeaderDto>> SearchAsync(SearchParams searchParams)
    {
        var query = searchSpecification.Apply(context.Concerts, searchParams);
        query = geometrySpecification.Apply(query, searchParams);
        var dtos = sortSpecification.Apply(
            query.ToHeaderDtos(context.ConcertRatingProjections),
            searchParams);
        return await dtos.ToPaginationAsync(searchParams);
    }

    public async Task<IEnumerable<ConcertHeaderDto>> GetByAmountAsync(int amount) =>
        await context.Concerts.Active(timeProvider.GetUtcNow().DateTime)
            .OrderByDescending(c => c.DatePosted)
            .ToHeaderDtos(context.ConcertRatingProjections)
            .Take(amount)
            .ToListAsync();

    public async Task<IEnumerable<ConcertHeaderDto>> GetPopularAsync() =>
        await context.Concerts.Active(timeProvider.GetUtcNow().DateTime)
            .OrderByDescending(c => c.TotalTickets - c.AvailableTickets)
            .ToHeaderDtos(context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();

    public async Task<IEnumerable<ConcertHeaderDto>> GetFreeAsync() =>
        await context.Concerts.Active(timeProvider.GetUtcNow().DateTime)
            .Where(c => c.Price == 0)
            .OrderByDescending(c => c.DatePosted)
            .ToHeaderDtos(context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();

    public async Task<IEnumerable<ConcertHeaderDto>> GetRecommendedAsync(ConcertParams concertParams)
    {
        var query = context.Concerts.Active(timeProvider.GetUtcNow().DateTime);

        if (concertParams.Genres.Any())
            query = query.Where(c => c.ConcertGenres.Any(eg => concertParams.Genres.Contains(eg.Genre)));

        query = geometrySpecification.Apply(query, concertParams);

        query = concertParams.OrderByRecent
            ? query.OrderByDescending(c => c.DatePosted)
            : query.OrderBy(c => c.StartDate);

        return await query
            .ToHeaderDtos(context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();
    }
}

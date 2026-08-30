using Concertable.Contracts;
using Concertable.DataAccess;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.Search.Infrastructure.Mappers;
using Concertable.Search.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ConcertHeaderRepository : IConcertHeaderRepository
{
    private readonly ISearchDbContext context;
    private readonly IConcertSearchQuery searchQuery;
    private readonly IGeometrySpecification<ConcertReadModel> geometrySpecification;
    private readonly TimeProvider timeProvider;

    public ConcertHeaderRepository(
        ISearchDbContext context,
        IConcertSearchQuery searchQuery,
        IGeometrySpecification<ConcertReadModel> geometrySpecification,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.searchQuery = searchQuery;
        this.geometrySpecification = geometrySpecification;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<ConcertHeader>> SearchAsync(SearchParams searchParams)
    {
        return await this.searchQuery
            .Apply(this.context.Concerts, searchParams)
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .ToPaginationAsync(searchParams);
    }

    public async Task<IReadOnlyList<ConcertHeader>> GetByAmountAsync(int amount) =>
        await context.Concerts.Active(timeProvider.GetUtcNow().DateTime)
            .OrderByDescending(c => c.DatePosted)
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(amount)
            .ToListAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetPopularAsync() =>
        await context.Concerts.Active(timeProvider.GetUtcNow().DateTime)
            .OrderByDescending(c => c.TotalTickets - c.AvailableTickets)
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetFreeAsync() =>
        await context.Concerts.Active(timeProvider.GetUtcNow().DateTime)
            .Where(c => c.Price == 0)
            .OrderByDescending(c => c.DatePosted)
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams)
    {
        var query = context.Concerts.Active(timeProvider.GetUtcNow().DateTime);

        if (concertParams.Genres.Any())
            query = query.Where(c => c.ConcertGenres.Any(eg => concertParams.Genres.Contains(eg.Genre)));

        query = query.Where(this.geometrySpecification.ToExpression(concertParams));

        query = concertParams.OrderByRecent
            ? query.OrderByDescending(c => c.DatePosted)
            : query.OrderBy(c => c.StartDate);

        return await query
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();
    }
}

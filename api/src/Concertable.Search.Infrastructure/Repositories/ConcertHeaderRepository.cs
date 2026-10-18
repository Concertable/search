using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IConcertSearchQueryBuilder queryBuilder;
    private readonly IGeometryProvider geometryProvider;
    private readonly TimeProvider timeProvider;

    public ConcertHeaderRepository(
        ISearchDbContext context,
        IConcertSearchQueryBuilder queryBuilder,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.queryBuilder = queryBuilder;
        this.geometryProvider = geometryProvider;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<ConcertHeader>> SearchAsync(SearchParams searchParams)
    {
        return await this.queryBuilder
            .Build(this.context.Concerts, searchParams)
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .ToPaginationAsync(searchParams);
    }

    public async Task<IReadOnlyList<ConcertHeader>> GetByAmountAsync(int amount) =>
        await context.Concerts.Active(timeProvider.GetUtcNow().UtcDateTime)
            .OrderByDatePostedDescending()
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(amount)
            .ToListAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetPopularAsync() =>
        await context.Concerts.Active(timeProvider.GetUtcNow().UtcDateTime)
            .OrderByDescending(c => c.TotalTickets - c.AvailableTickets)
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetFreeAsync() =>
        await context.Concerts.Active(timeProvider.GetUtcNow().UtcDateTime)
            .Where(c => c.Price == 0)
            .OrderByDatePostedDescending()
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams)
    {
        var query = context.Concerts.Active(timeProvider.GetUtcNow().UtcDateTime);

        if (concertParams.Genres.Any())
            query = query.Where(c => c.ConcertGenres.Any(eg => concertParams.Genres.Contains(eg.Genre)));

        var center = concertParams.HasValidCoordinates()
            ? geometryProvider.CreatePoint(concertParams.Latitude, concertParams.Longitude)
            : null;
        var geometry = new GeometrySpecification<ConcertReadModel>(center, (concertParams.RadiusKm ?? 10) * 1000);
        query = query.Where(geometry.ToExpression());

        query = concertParams.OrderByRecent
            ? query.OrderByDatePostedDescending()
            : query.OrderBy(c => c.StartDate);

        return await query
            .ToHeaderDtos(context.Artists, context.Venues, context.ConcertRatingProjections)
            .Take(10)
            .ToListAsync();
    }
}

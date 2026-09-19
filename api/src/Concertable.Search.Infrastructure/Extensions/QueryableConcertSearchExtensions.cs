using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Extensions;

internal static class QueryableConcertSearchExtensions
{
    public static IQueryable<ConcertReadModel> Active(this IQueryable<ConcertReadModel> query, DateTime now) =>
        query.Where(c => c.DatePosted != null && c.EndDate > now);

    public static IOrderedQueryable<ConcertReadModel> OrderByDatePostedDescending(
        this IQueryable<ConcertReadModel> query) =>
        query.OrderBy(concert => concert.DatePosted == null)
            .ThenByDescending(concert => concert.DatePosted);
}

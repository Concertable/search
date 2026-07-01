namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSortSpecification : ISortSpecification<ConcertReadModel>
{
    public IQueryable<ConcertReadModel> Apply(IQueryable<ConcertReadModel> query, Sort? sort) =>
        sort switch
        {
            { Field: SortField.Name, Direction: SortDirection.Asc } => query.OrderBy(c => c.Name),
            { Field: SortField.Name, Direction: SortDirection.Desc } => query.OrderByDescending(c => c.Name),
            { Field: SortField.Date, Direction: SortDirection.Asc } => query.OrderBy(c => c.StartDate),
            { Field: SortField.Date, Direction: SortDirection.Desc } => query.OrderByDescending(c => c.StartDate),
            _ => query.OrderBy(c => c.StartDate)
        };
}

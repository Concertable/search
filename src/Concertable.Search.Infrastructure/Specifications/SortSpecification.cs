namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SortSpecification<T> : ISortSpecification<T>
    where T : class, IIdEntity, IHasName
{
    public IQueryable<T> Apply(IQueryable<T> query, Sort? sort) =>
        sort switch
        {
            { Field: SortField.Name, Direction: SortDirection.Asc } => query.OrderBy(e => e.Name),
            { Field: SortField.Name, Direction: SortDirection.Desc } => query.OrderByDescending(e => e.Name),
            _ => query.OrderBy(e => e.Id)
        };
}

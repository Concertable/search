namespace Concertable.Search.Application.Interfaces;

internal interface ISortSpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query, ISortParams sortParams);
}

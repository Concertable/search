namespace Concertable.Search.Application.Interfaces;

internal interface ISearchSpecification<TEntity>
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, string? searchTerm);
}

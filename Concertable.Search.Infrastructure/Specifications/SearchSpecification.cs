namespace Concertable.Search.Infrastructure.Specifications;

internal class SearchSpecification<TEntity> : ISearchSpecification<TEntity>
    where TEntity : IEntity, IHasName
{
    public IQueryable<TEntity> Apply(IQueryable<TEntity> query, string? searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(e => e.Name.Contains(searchTerm));

        return query;
    }
}

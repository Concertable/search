using Concertable.Kernel;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SearchSpecification<TEntity> : ISearchSpecification<TEntity>
    where TEntity : class, IEntity, IHasName
{
    public IQueryable<TEntity> Apply(IQueryable<TEntity> query, string? searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(e => e.Name.Contains(searchTerm));

        return query;
    }
}

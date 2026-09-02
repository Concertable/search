using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SearchTermSpecification<TEntity>
    : PredicateSpecification<TEntity, string?>, INameSpecification<TEntity>
    where TEntity : class, IHasName
{
    protected override Expression<Func<TEntity, bool>> Predicate(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return _ => true;

        return entity => entity.Name.Contains(searchTerm);
    }
}

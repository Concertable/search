using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SearchTermSpecification<TEntity> : PredicateSpecification<TEntity>
    where TEntity : class, IHasName
{
    public SearchTermSpecification(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Predicate = _ => true;
            return;
        }

        var normalized = searchTerm.ToLowerInvariant();
        Predicate = entity => EF.Functions.Collate(entity.Name.ToLower(), "C").Contains(normalized);
    }

    protected override Expression<Func<TEntity, bool>> Predicate { get; }
}

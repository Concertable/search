using System.Linq.Expressions;
using Concertable.Contracts;
using Concertable.Kernel.Expressions;
using Concertable.Kernel.Specifications;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class GenreSpecification<TEntity> : PredicateSpecification<TEntity>
    where TEntity : class
{
    public GenreSpecification(
        Expression<Func<TEntity, IEnumerable<Genre>>> genres,
        IEnumerable<Genre> selectedGenres)
    {
        var selected = selectedGenres.ToArray();
        Predicate = selected.Length == 0
            ? _ => true
            : genres.Substitute(available => available.Any(genre => selected.Contains(genre)));
    }

    protected override Expression<Func<TEntity, bool>> Predicate { get; }
}

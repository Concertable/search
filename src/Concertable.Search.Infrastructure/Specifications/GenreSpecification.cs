using System.Linq.Expressions;
using Concertable.Contracts;
using Concertable.Kernel.Expressions;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class GenreSpecification<TEntity>
    : PredicateSpecification<TEntity, IGenreParams>, IGenreSpecification<TEntity>
    where TEntity : class
{
    private readonly Expression<Func<TEntity, IEnumerable<Genre>>> genres;

    public GenreSpecification(Expression<Func<TEntity, IEnumerable<Genre>>> genres)
    {
        this.genres = genres;
    }

    protected override Expression<Func<TEntity, bool>> Predicate(IGenreParams @params)
    {
        if (@params.Genres.Count == 0)
            return _ => true;

        return this.genres.Substitute(available => available.Any(genre => @params.Genres.Contains(genre)));
    }
}

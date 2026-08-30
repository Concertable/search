using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Expressions;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SearchSpecification<TEntity> : ISearchSpecification<TEntity>
    where TEntity : class, IIdEntity, IHasName, IHasLocation
{
    private readonly INameSpecification<TEntity> nameSpecification;
    private readonly IGenreSpecification<TEntity> genreSpecification;
    private readonly IGeometrySpecification<TEntity> geometrySpecification;

    public SearchSpecification(
        INameSpecification<TEntity> nameSpecification,
        IGenreSpecification<TEntity> genreSpecification,
        IGeometrySpecification<TEntity> geometrySpecification)
    {
        this.nameSpecification = nameSpecification;
        this.genreSpecification = genreSpecification;
        this.geometrySpecification = geometrySpecification;
    }

    public Expression<Func<TEntity, bool>> ToExpression(SearchParams @params) =>
        this.nameSpecification.ToExpression(@params.SearchTerm)
            .And(this.genreSpecification.ToExpression(@params))
            .And(this.geometrySpecification.ToExpression(@params));
}

using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Expressions;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SearchSpecification<TEntity> : ISearchSpecification<TEntity>
    where TEntity : class, IIdEntity, IHasName, IHasLocation
{
    private readonly INameSpecification<TEntity> nameSpec;
    private readonly IGenreSpecification<TEntity> genreSpec;
    private readonly IGeometrySpecification<TEntity> geometrySpec;

    public SearchSpecification(
        INameSpecification<TEntity> nameSpec,
        IGenreSpecification<TEntity> genreSpec,
        IGeometrySpecification<TEntity> geometrySpec)
    {
        this.nameSpec = nameSpec;
        this.genreSpec = genreSpec;
        this.geometrySpec = geometrySpec;
    }

    public Expression<Func<TEntity, bool>> ToExpression(SearchParams @params) =>
        this.nameSpec.ToExpression(@params.SearchTerm)
            .And(this.genreSpec.ToExpression(@params))
            .And(this.geometrySpec.ToExpression(@params));
}

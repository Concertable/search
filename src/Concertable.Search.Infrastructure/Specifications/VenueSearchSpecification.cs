using System.Linq.Expressions;
using Concertable.Kernel.Expressions;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class VenueSearchSpecification : ISearchSpecification<VenueReadModel>
{
    private readonly INameSpecification<VenueReadModel> nameSpec;
    private readonly IGeometrySpecification<VenueReadModel> geometrySpec;

    public VenueSearchSpecification(
        INameSpecification<VenueReadModel> nameSpec,
        IGeometrySpecification<VenueReadModel> geometrySpec)
    {
        this.nameSpec = nameSpec;
        this.geometrySpec = geometrySpec;
    }

    public Expression<Func<VenueReadModel, bool>> ToExpression(SearchParams @params) =>
        this.nameSpec.ToExpression(@params.SearchTerm)
            .And(this.geometrySpec.ToExpression(@params));
}

using System.Linq.Expressions;
using Concertable.Kernel.Expressions;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class VenueSearchSpecification : ISearchSpecification<VenueReadModel>
{
    private readonly INameSpecification<VenueReadModel> nameSpecification;
    private readonly IGeometrySpecification<VenueReadModel> geometrySpecification;

    public VenueSearchSpecification(
        INameSpecification<VenueReadModel> nameSpecification,
        IGeometrySpecification<VenueReadModel> geometrySpecification)
    {
        this.nameSpecification = nameSpecification;
        this.geometrySpecification = geometrySpecification;
    }

    public Expression<Func<VenueReadModel, bool>> ToExpression(SearchParams @params) =>
        this.nameSpecification.ToExpression(@params.SearchTerm)
            .And(this.geometrySpecification.ToExpression(@params));
}

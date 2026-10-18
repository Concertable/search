using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.QueryBuilders;

internal sealed class ArtistSearchQueryBuilder : IArtistSearchQueryBuilder
{
    private readonly IGeometryProvider geometryProvider;

    public ArtistSearchQueryBuilder(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.geometryProvider = geometryProvider;
    }

    public IQueryable<ArtistReadModel> Build(IQueryable<ArtistReadModel> query, SearchParams @params)
    {
        var center = @params.HasValidCoordinates()
            ? geometryProvider.CreatePoint(@params.Latitude, @params.Longitude)
            : null;
        var search = new SearchTermSpecification<ArtistReadModel>(@params.SearchTerm)
            .And(new GeometrySpecification<ArtistReadModel>(center, (@params.RadiusKm ?? 10) * 1000))
            .And(new GenreSpecification<ArtistReadModel>(
                ArtistSearchGenreSelectors.FromArtist, @params.Genres));

        return query
            .Where(search.ToExpression())
            .ApplyOrders(new SortSpecification<ArtistReadModel>(@params.Sort));
    }
}

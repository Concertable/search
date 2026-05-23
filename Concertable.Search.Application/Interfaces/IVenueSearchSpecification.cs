using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IVenueSearchSpecification
{
    IQueryable<VenueReadModel> Apply(IQueryable<VenueReadModel> query, SearchParams searchParams);
}

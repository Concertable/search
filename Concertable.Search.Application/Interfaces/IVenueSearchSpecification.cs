namespace Concertable.Search.Application.Interfaces;

internal interface IVenueSearchSpecification
{
    IQueryable<VenueSearchModel> Apply(IQueryable<VenueSearchModel> query, SearchParams searchParams);
}

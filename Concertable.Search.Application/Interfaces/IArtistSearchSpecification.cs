namespace Concertable.Search.Application.Interfaces;

internal interface IArtistSearchSpecification
{
    IQueryable<ArtistSearchModel> Apply(IQueryable<ArtistSearchModel> query, SearchParams searchParams);
}

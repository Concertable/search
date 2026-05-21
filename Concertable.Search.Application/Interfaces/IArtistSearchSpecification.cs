namespace Concertable.Search.Application.Interfaces;

internal interface IArtistSearchSpecification
{
    IQueryable<ArtistReadModel> Apply(IQueryable<ArtistReadModel> query, SearchParams searchParams);
}

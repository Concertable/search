using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IArtistSearchSpecification
{
    IQueryable<ArtistReadModel> Apply(IQueryable<ArtistReadModel> query, SearchParams searchParams);
}

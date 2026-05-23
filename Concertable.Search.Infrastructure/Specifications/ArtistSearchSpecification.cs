using Concertable.Search.Application.Interfaces;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Specifications;

internal class ArtistSearchSpecification : IArtistSearchSpecification
{
    private readonly ISearchSpecification<ArtistReadModel> searchSpecification;

    public ArtistSearchSpecification(ISearchSpecification<ArtistReadModel> searchSpecification)
    {
        this.searchSpecification = searchSpecification;
    }

    public IQueryable<ArtistReadModel> Apply(IQueryable<ArtistReadModel> query, SearchParams searchParams)
    {
        if (searchParams.Genres?.Any() == true)
            query = query.Where(a => a.ArtistGenres.Any(ag => searchParams.Genres.Contains(ag.Genre)));

        return searchSpecification.Apply(query, searchParams.SearchTerm);
    }
}

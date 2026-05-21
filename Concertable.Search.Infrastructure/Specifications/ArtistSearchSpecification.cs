using Concertable.Search.Application.Interfaces;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Specifications;

internal class ArtistSearchSpecification : IArtistSearchSpecification
{
    private readonly ISearchSpecification<ArtistSearchModel> searchSpecification;

    public ArtistSearchSpecification(ISearchSpecification<ArtistSearchModel> searchSpecification)
    {
        this.searchSpecification = searchSpecification;
    }

    public IQueryable<ArtistSearchModel> Apply(IQueryable<ArtistSearchModel> query, SearchParams searchParams)
    {
        if (searchParams.Genres?.Any() == true)
            query = query.Where(a => a.ArtistGenres.Any(ag => searchParams.Genres.Contains(ag.Genre)));

        return searchSpecification.Apply(query, searchParams.SearchTerm);
    }
}

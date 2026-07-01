using Concertable.Search.Application.Interfaces;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSearchSpecification : IConcertSearchSpecification
{
    private readonly ISearchSpecification<ConcertReadModel> searchSpecification;
    private readonly TimeProvider timeProvider;

    public ConcertSearchSpecification(
        ISearchSpecification<ConcertReadModel> searchSpecification,
        TimeProvider timeProvider)
    {
        this.searchSpecification = searchSpecification;
        this.timeProvider = timeProvider;
    }

    public IQueryable<ConcertReadModel> Apply(IQueryable<ConcertReadModel> query, SearchParams searchParams)
    {
        query = query
            .Where(e => e.DatePosted != null)
            .Where(e => e.EndDate > timeProvider.GetUtcNow());

        if (searchParams.From != null)
            query = query.Where(e => DateOnly.FromDateTime(e.StartDate) >= searchParams.From);

        if (searchParams.Genres?.Any() == true)
            query = query.Where(e => e.ConcertGenres.Any(eg => searchParams.Genres.Contains(eg.Genre)));

        if (searchParams.ShowHistory == false)
            query = query.Where(e => e.StartDate >= timeProvider.GetUtcNow());

        if (searchParams.ShowSold == false)
            query = query.Where(e => e.AvailableTickets > 0);

        return searchSpecification.Apply(query, searchParams.SearchTerm);
    }
}

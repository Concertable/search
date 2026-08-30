using System.Linq.Expressions;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSearchSpecification
    : PredicateSpecification<ConcertReadModel, SearchParams>, IConcertSearchSpecification
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

    protected override Expression<Func<ConcertReadModel, bool>> Predicate(SearchParams @params)
    {
        var now = this.timeProvider.GetUtcNow();

        return this.searchSpecification
            .And(concert =>
                concert.DatePosted != null
                && concert.EndDate > now
                && (@params.From == null || DateOnly.FromDateTime(concert.StartDate) >= @params.From)
                && (@params.ShowHistory != false || concert.StartDate >= now)
                && (@params.ShowSold != false || concert.AvailableTickets > 0))
            .ToExpression(@params);
    }
}

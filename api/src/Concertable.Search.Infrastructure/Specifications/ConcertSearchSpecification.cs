using System.Linq.Expressions;
using Concertable.Kernel.Specifications;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSearchSpecification : PredicateSpecification<ConcertReadModel>
{
    public ConcertSearchSpecification(
        IPredicateSpecification<ConcertReadModel> search,
        DateTime now,
        DateOnly? from,
        bool? showHistory,
        bool? showSold)
    {
        Predicate = search.And(concert =>
            concert.DatePosted != null
            && concert.EndDate > now
            && (from == null || DateOnly.FromDateTime(concert.StartDate) >= from)
            && (showHistory != false || concert.StartDate >= now)
            && (showSold != false || concert.AvailableTickets > 0))
            .ToExpression();
    }

    protected override Expression<Func<ConcertReadModel, bool>> Predicate { get; }
}

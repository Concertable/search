using Concertable.Search.Domain.Models;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertSearchSpecification
{
    IQueryable<ConcertSearchModel> Apply(IQueryable<ConcertSearchModel> query, SearchParams searchParams);
}

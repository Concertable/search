using Concertable.Search.Application.Params;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertSearchSpecification
{
    IQueryable<ConcertReadModel> Apply(IQueryable<ConcertReadModel> query, SearchParams searchParams);
}

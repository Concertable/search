
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSortSpecification : ISortSpecification<ConcertHeader>
{
    public IQueryable<ConcertHeader> Apply(IQueryable<ConcertHeader> query, ISortParams sortParams) =>
        sortParams.Sort?.ToLower() switch
        {
            "name_asc" => query.OrderBy(c => c.Name),
            "name_desc" => query.OrderByDescending(c => c.Name),
            "date_asc" => query.OrderBy(c => c.StartDate),
            "date_desc" => query.OrderByDescending(c => c.StartDate),
            _ => query.OrderBy(c => c.StartDate)
        };
}

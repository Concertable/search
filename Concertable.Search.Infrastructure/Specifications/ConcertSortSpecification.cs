
using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Infrastructure.Specifications;

internal class ConcertSortSpecification : ISortSpecification<ConcertHeaderDto>
{
    public IQueryable<ConcertHeaderDto> Apply(IQueryable<ConcertHeaderDto> query, ISortParams sortParams) =>
        sortParams.Sort?.ToLower() switch
        {
            "name_asc" => query.OrderBy(c => c.Name),
            "name_desc" => query.OrderByDescending(c => c.Name),
            "date_asc" => query.OrderBy(c => c.StartDate),
            "date_desc" => query.OrderByDescending(c => c.StartDate),
            _ => query.OrderBy(c => c.StartDate)
        };
}

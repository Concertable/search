using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSortSpecification : ISortSpecification<ConcertReadModel>
{
    public IReadOnlyList<SpecificationOrder<ConcertReadModel>> ToOrders(Sort? @params) =>
        @params switch
        {
            { Field: SortField.Name, Direction: SortDirection.Asc } => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.Name, SpecificationOrderDirection.Ascending)],
            { Field: SortField.Name, Direction: SortDirection.Desc } => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.Name, SpecificationOrderDirection.Descending)],
            { Field: SortField.Date, Direction: SortDirection.Asc } => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.StartDate, SpecificationOrderDirection.Ascending)],
            { Field: SortField.Date, Direction: SortDirection.Desc } => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.StartDate, SpecificationOrderDirection.Descending)],
            _ => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.StartDate, SpecificationOrderDirection.Ascending)]
        };
}

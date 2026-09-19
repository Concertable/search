using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class ConcertSortSpecification : ISortSpecification<ConcertReadModel>
{
    public IReadOnlyList<SpecificationOrder<ConcertReadModel>> ToOrders(Sort? @params) =>
        @params switch
        {
            { Field: SortField.Name, Direction: SortDirection.Asc } =>
            [
                SpecificationOrder<ConcertReadModel>.Create(concert => EF.Functions.Collate(concert.Name.ToLower(), "C"), SpecificationOrderDirection.Ascending),
                SpecificationOrder<ConcertReadModel>.Create(concert => EF.Functions.Collate(concert.Name, "C"), SpecificationOrderDirection.Ascending)
            ],
            { Field: SortField.Name, Direction: SortDirection.Desc } =>
            [
                SpecificationOrder<ConcertReadModel>.Create(concert => EF.Functions.Collate(concert.Name.ToLower(), "C"), SpecificationOrderDirection.Descending),
                SpecificationOrder<ConcertReadModel>.Create(concert => EF.Functions.Collate(concert.Name, "C"), SpecificationOrderDirection.Descending)
            ],
            { Field: SortField.Date, Direction: SortDirection.Asc } => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.StartDate, SpecificationOrderDirection.Ascending)],
            { Field: SortField.Date, Direction: SortDirection.Desc } => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.StartDate, SpecificationOrderDirection.Descending)],
            _ => [SpecificationOrder<ConcertReadModel>.Create(concert => concert.StartDate, SpecificationOrderDirection.Ascending)]
        };
}

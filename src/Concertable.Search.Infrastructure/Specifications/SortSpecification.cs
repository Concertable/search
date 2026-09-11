using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class SortSpecification<TEntity> : ISortSpecification<TEntity>
    where TEntity : class, IIdEntity, IHasName
{
    public IReadOnlyList<SpecificationOrder<TEntity>> ToOrders(Sort? @params) =>
        @params switch
        {
            { Field: SortField.Name, Direction: SortDirection.Asc } => [SpecificationOrder<TEntity>.Create(entity => entity.Name, SpecificationOrderDirection.Ascending)],
            { Field: SortField.Name, Direction: SortDirection.Desc } => [SpecificationOrder<TEntity>.Create(entity => entity.Name, SpecificationOrderDirection.Descending)],
            _ => [SpecificationOrder<TEntity>.Create(entity => entity.Id, SpecificationOrderDirection.Ascending)]
        };
}

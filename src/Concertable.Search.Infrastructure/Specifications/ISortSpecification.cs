using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal interface ISortSpecification<TEntity> : ISpecification
    where TEntity : class
{
    IReadOnlyList<SpecificationOrder<TEntity>> ToOrders(Sort? @params);
}

using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal interface ISearchSpecification<TEntity> : IPredicateSpecification<TEntity, SearchParams>
    where TEntity : class;

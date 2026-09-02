using Concertable.Kernel.Specifications;

namespace Concertable.Search.Infrastructure.Specifications;

internal interface INameSpecification<TEntity> : IPredicateSpecification<TEntity, string?>
    where TEntity : class;

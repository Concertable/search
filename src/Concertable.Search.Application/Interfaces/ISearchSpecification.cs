using Concertable.Kernel.Specifications;

namespace Concertable.Search.Application.Interfaces;

internal interface ISearchSpecification<TEntity> : ISpecification<TEntity, string?>
    where TEntity : class;

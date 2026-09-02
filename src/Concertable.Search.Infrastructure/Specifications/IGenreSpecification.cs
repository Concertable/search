using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal interface IGenreSpecification<TEntity> : IPredicateSpecification<TEntity, IGenreParams>
    where TEntity : class;

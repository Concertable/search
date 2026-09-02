using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Infrastructure.Specifications;

internal interface IGeometrySpecification<TEntity> : IPredicateSpecification<TEntity, IGeoParams>
    where TEntity : class, IIdEntity, IHasLocation;

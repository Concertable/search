using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IGeometrySpecification<TEntity> : ISpecification<TEntity, IGeoParams>
    where TEntity : class, IIdEntity, IHasLocation;

using Concertable.Kernel.Specifications;

namespace Concertable.Search.Application.Interfaces;

internal interface ISortSpecification<T> : ISpecification<T, ISortParams>
    where T : class;

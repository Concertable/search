using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface ISortSpecification<T> : ISpecification<T, Sort?>
    where T : class;

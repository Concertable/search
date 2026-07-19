using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertSearchSpecification : ISpecification<ConcertReadModel, SearchParams>;

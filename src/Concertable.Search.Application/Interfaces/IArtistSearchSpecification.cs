using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IArtistSearchSpecification : ISpecification<ArtistReadModel, SearchParams>;

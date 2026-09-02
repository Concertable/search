using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Queries;

internal interface IVenueSearchQuery : IQuery<VenueReadModel, SearchParams>;

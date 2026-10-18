using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.QueryBuilders;

internal interface IVenueSearchQueryBuilder : IQueryBuilder<VenueReadModel, SearchParams>;



using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IVenueHeaderRepository : IHeaderRepository<VenueHeader>
{
    Task<IReadOnlyList<VenueHeader>> GetByAmountAsync(int amount);
}

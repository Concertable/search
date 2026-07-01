

using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IVenueHeaderRepository : IHeaderRepository<VenueHeader>
{
    Task<IEnumerable<VenueHeader>> GetByAmountAsync(int amount);
}



namespace Concertable.Search.Application.Interfaces;

internal interface IVenueHeaderRepository : IHeaderRepository<VenueHeaderDto>
{
    Task<IEnumerable<VenueHeaderDto>> GetByAmountAsync(int amount);
}

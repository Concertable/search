

namespace Concertable.Search.Application.Interfaces;

internal interface IArtistHeaderRepository : IHeaderRepository<ArtistHeaderDto>
{
    Task<IEnumerable<ArtistHeaderDto>> GetByAmountAsync(int amount);
}



using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IArtistHeaderRepository : IHeaderRepository<ArtistHeader>
{
    Task<IEnumerable<ArtistHeader>> GetByAmountAsync(int amount);
}

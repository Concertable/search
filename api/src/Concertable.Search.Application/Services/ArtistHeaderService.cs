using Concertable.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Services;

internal sealed class ArtistHeaderService : IHeaderService
{
    private readonly IArtistHeaderRepository artistHeaderRepository;

    public ArtistHeaderService(IArtistHeaderRepository artistHeaderRepository)
    {
        this.artistHeaderRepository = artistHeaderRepository;
    }

    public async Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams)
    {
        // IPagination<out T> is covariant, so the repository's page IS an IPagination<IHeader>.
        return await artistHeaderRepository.SearchAsync(searchParams);
    }

    public async Task<IReadOnlyList<IHeader>> GetByAmountAsync(int amount) =>
        await artistHeaderRepository.GetByAmountAsync(amount);
}

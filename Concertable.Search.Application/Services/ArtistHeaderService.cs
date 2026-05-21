using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal class ArtistHeaderService : IHeaderService
{
    private readonly IArtistHeaderRepository artistHeaderRepository;

    public ArtistHeaderService(IArtistHeaderRepository artistHeaderRepository)
    {
        this.artistHeaderRepository = artistHeaderRepository;
    }

    public async Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams)
    {
        var result = await artistHeaderRepository.SearchAsync(searchParams);
        return new Pagination<ArtistHeaderDto>(result.Data, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(int amount) =>
        await artistHeaderRepository.GetByAmountAsync(amount);
}

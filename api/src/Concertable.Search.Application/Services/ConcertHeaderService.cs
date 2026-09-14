using Concertable.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Services;

internal sealed class ConcertHeaderService : IHeaderService, IConcertHeaderService
{
    private readonly IConcertHeaderRepository concertHeaderRepository;

    public ConcertHeaderService(IConcertHeaderRepository concertHeaderRepository)
    {
        this.concertHeaderRepository = concertHeaderRepository;
    }

    public async Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams)
    {
        // IPagination<out T> is covariant, so the repository's page IS an IPagination<IHeader>.
        return await concertHeaderRepository.SearchAsync(searchParams);
    }

    public async Task<IReadOnlyList<IHeader>> GetByAmountAsync(int amount) =>
        await concertHeaderRepository.GetByAmountAsync(amount);

    public async Task<IReadOnlyList<ConcertHeader>> GetPopularAsync() =>
        await concertHeaderRepository.GetPopularAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetFreeAsync() =>
        await concertHeaderRepository.GetFreeAsync();

    public async Task<IReadOnlyList<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams) =>
        await concertHeaderRepository.GetRecommendedAsync(concertParams);
}

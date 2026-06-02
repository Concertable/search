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
        var result = await concertHeaderRepository.SearchAsync(searchParams);
        return new Pagination<ConcertHeader>(result.Data, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(int amount) =>
        await concertHeaderRepository.GetByAmountAsync(amount);

    public async Task<IEnumerable<ConcertHeader>> GetPopularAsync() =>
        await concertHeaderRepository.GetPopularAsync();

    public async Task<IEnumerable<ConcertHeader>> GetFreeAsync() =>
        await concertHeaderRepository.GetFreeAsync();

    public async Task<IEnumerable<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams) =>
        await concertHeaderRepository.GetRecommendedAsync(concertParams);
}

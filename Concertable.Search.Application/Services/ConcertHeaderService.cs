using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal class ConcertHeaderService : IHeaderService, IConcertHeaderModule
{
    private readonly IConcertHeaderRepository concertHeaderRepository;

    public ConcertHeaderService(IConcertHeaderRepository concertHeaderRepository)
    {
        this.concertHeaderRepository = concertHeaderRepository;
    }

    public async Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams)
    {
        var result = await concertHeaderRepository.SearchAsync(searchParams);
        return new Pagination<ConcertHeaderDto>(result.Data, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(int amount) =>
        await concertHeaderRepository.GetByAmountAsync(amount);

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(HeaderType type, int amount) =>
        await concertHeaderRepository.GetByAmountAsync(amount);

    public async Task<IEnumerable<ConcertHeaderDto>> GetPopularAsync() =>
        await concertHeaderRepository.GetPopularAsync();

    public async Task<IEnumerable<ConcertHeaderDto>> GetFreeAsync() =>
        await concertHeaderRepository.GetFreeAsync();

    public async Task<IEnumerable<ConcertHeaderDto>> GetRecommendedAsync(ConcertParams concertParams) =>
        await concertHeaderRepository.GetRecommendedAsync(concertParams);
}

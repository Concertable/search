namespace Concertable.Search.Application.Interfaces;

internal interface IConcertHeaderRepository : IHeaderRepository<ConcertHeaderDto>
{
    Task<IEnumerable<ConcertHeaderDto>> GetByAmountAsync(int amount);
    Task<IEnumerable<ConcertHeaderDto>> GetPopularAsync();
    Task<IEnumerable<ConcertHeaderDto>> GetFreeAsync();
    Task<IEnumerable<ConcertHeaderDto>> GetRecommendedAsync(ConcertParams concertParams);
}

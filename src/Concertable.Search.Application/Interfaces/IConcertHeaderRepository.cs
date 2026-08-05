using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertHeaderRepository : IHeaderRepository<ConcertHeader>
{
    Task<IReadOnlyList<ConcertHeader>> GetByAmountAsync(int amount);
    Task<IReadOnlyList<ConcertHeader>> GetPopularAsync();
    Task<IReadOnlyList<ConcertHeader>> GetFreeAsync();
    Task<IReadOnlyList<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams);
}

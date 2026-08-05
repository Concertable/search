using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertHeaderService : IHeaderService
{
    Task<IReadOnlyList<ConcertHeader>> GetPopularAsync();
    Task<IReadOnlyList<ConcertHeader>> GetFreeAsync();
    Task<IReadOnlyList<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams);
}

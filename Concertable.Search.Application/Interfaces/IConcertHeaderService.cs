using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertHeaderService : IHeaderService
{
    Task<IEnumerable<ConcertHeader>> GetPopularAsync();
    Task<IEnumerable<ConcertHeader>> GetFreeAsync();
    Task<IEnumerable<ConcertHeader>> GetRecommendedAsync(ConcertParams concertParams);
}

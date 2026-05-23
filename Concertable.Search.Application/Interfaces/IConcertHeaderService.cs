using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertHeaderService : IHeaderService
{
    Task<IEnumerable<ConcertHeaderDto>> GetPopularAsync();
    Task<IEnumerable<ConcertHeaderDto>> GetFreeAsync();
    Task<IEnumerable<ConcertHeaderDto>> GetRecommendedAsync(ConcertParams concertParams);
}

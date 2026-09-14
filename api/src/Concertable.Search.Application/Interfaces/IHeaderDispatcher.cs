using Concertable.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IHeaderDispatcher
{
    Task<IPagination<IHeader>> SearchAsync(HeaderType type, SearchParams searchParams);
    Task<IReadOnlyList<IHeader>> GetByAmountAsync(HeaderType type, int amount);
}

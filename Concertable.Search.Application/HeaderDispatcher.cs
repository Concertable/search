using Concertable.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application;

internal sealed class HeaderDispatcher : IHeaderDispatcher
{
    private readonly IHeaderServiceFactory headerServiceFactory;

    public HeaderDispatcher(IHeaderServiceFactory headerServiceFactory)
    {
        this.headerServiceFactory = headerServiceFactory;
    }

    public async Task<IPagination<IHeader>> SearchAsync(HeaderType type, SearchParams searchParams)
    {
        var service = headerServiceFactory.Create(type);
        return await service.SearchAsync(searchParams);
    }

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(HeaderType type, int amount)
    {
        var service = headerServiceFactory.Create(type);
        return await service.GetByAmountAsync(amount);
    }
}

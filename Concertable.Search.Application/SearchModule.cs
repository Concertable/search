using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application;

internal class SearchModule : IHeaderModule
{
    private readonly IHeaderServiceFactory headerServiceFactory;

    public SearchModule(IHeaderServiceFactory headerServiceFactory)
    {
        this.headerServiceFactory = headerServiceFactory;
    }

    public async Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams)
    {
        var service = headerServiceFactory.Create(searchParams.HeaderType!.Value);
        return await service.SearchAsync(searchParams);
    }

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(HeaderType type, int amount)
    {
        var service = headerServiceFactory.Create(type);
        return await service.GetByAmountAsync(amount);
    }
}

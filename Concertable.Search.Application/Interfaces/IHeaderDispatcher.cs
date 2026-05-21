namespace Concertable.Search.Application.Interfaces;

internal interface IHeaderDispatcher
{
    Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams);
    Task<IEnumerable<IHeader>> GetByAmountAsync(HeaderType type, int amount);
}

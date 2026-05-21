namespace Concertable.Search.Application.Interfaces;

internal interface IHeaderModule
{
    Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams);
    Task<IEnumerable<IHeader>> GetByAmountAsync(HeaderType type, int amount);
}

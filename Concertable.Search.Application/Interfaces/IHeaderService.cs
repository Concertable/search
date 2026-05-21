namespace Concertable.Search.Application.Interfaces;

internal interface IHeaderService
{
    Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams);
    Task<IEnumerable<IHeader>> GetByAmountAsync(int amount);
}

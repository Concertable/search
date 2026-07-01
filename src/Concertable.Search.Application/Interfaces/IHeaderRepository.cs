using Concertable.Contracts;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Interfaces;

internal interface IHeaderRepository<TEntity>
{
    Task<IPagination<TEntity>> SearchAsync(SearchParams searchParams);
}

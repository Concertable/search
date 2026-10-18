namespace Concertable.Search.Infrastructure.QueryBuilders;

internal interface IQueryBuilder<TEntity, in TParams>
    where TEntity : class
{
    IQueryable<TEntity> Build(IQueryable<TEntity> query, TParams @params);
}

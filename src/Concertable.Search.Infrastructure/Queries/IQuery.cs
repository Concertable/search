namespace Concertable.Search.Infrastructure.Queries;

internal interface IQuery<TEntity, in TParams>
    where TEntity : class
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, TParams @params);
}

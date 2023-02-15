using System.Linq.Expressions;

namespace DataOnion.db
{
    public interface IEntity<TIndex>
    {
        TIndex Id { get; set; }
    }

    public interface IEFCoreService<TDbContext>
    {
        TDbContext DbContext { get; }
        // crud
        Task<TEntity> CreateAsync<TEntity>(
            TEntity entity
        ) where TEntity : class;
        Task<TEntity> UpdateAsync<TEntity, TIndex>(
            TIndex id, TEntity entity
        ) where TEntity : class, IEntity<TIndex>;
        Task DeleteAsync<TEntity>(
            TEntity entity
        ) where TEntity : class;
        Task<TEntity?> FetchAsync<TEntity, TIndex>(
            TIndex id
        ) where TEntity : class;
        Task<TEntity?> FetchAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate
        ) where TEntity : class;
        Task<TResult?> FetchAsync<TEntity, TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector
        ) where TEntity : class;
        Task<IEnumerable<TEntity>> WhereAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate
        ) where TEntity : class;
        IQueryable<TEntity> QueryableWhere<TEntity>(
            Expression<Func<TEntity, bool>> predicate
        ) where TEntity : class;
    }
}
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DataOnion.db
{
    public class EFCoreService<TDbContext> : IEFCoreService<TDbContext> 
        where TDbContext : DbContext
    {
        private readonly TDbContext _context;

        public EFCoreService(
            TDbContext context
        )
        {
            _context = context;
        }

        public async Task<TEntity> CreateAsync<TEntity>(TEntity entity)
            where TEntity : class
        {
            var dbEntity = _context.Add<TEntity>(entity);
            
            await _context.SaveChangesAsync();

            return dbEntity.Entity;
        }

        public async Task<TEntity?> FetchAsync<TEntity, TIndex>(TIndex id)
            where TEntity : class
        {
            var entity = await _context.Set<TEntity>()
                .FindAsync(id);

            return entity;
        }

        public async Task<TEntity?> FetchAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate
        ) where TEntity : class
        {
            var results = await _context.Set<TEntity>()
                .Where(predicate)
                .FirstOrDefaultAsync();

            return results;
        }

        public async Task<TResult?> FetchAsync<TEntity, TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector
        ) where TEntity : class
        {
            var results = await _context.Set<TEntity>()
                .Where(predicate)
                .Select(selector)
                .FirstOrDefaultAsync();

            return results;
        }

        public async Task<IEnumerable<TEntity>> WhereAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate
        ) where TEntity : class
        {
            return await QueryableWhere(predicate).ToListAsync();
        }

        public async Task<TEntity> UpdateAsync<TEntity, TIndex>(
            TIndex id, 
            TEntity entity
        ) where TEntity : class, IEntity<TIndex>
        {
            entity.Id = id;

            var ret = _context.Set<TEntity>().Update(entity);

            await _context.SaveChangesAsync();

            return ret.Entity;
        }

        public IQueryable<TEntity> QueryableWhere<TEntity>(
            Expression<Func<TEntity, bool>> predicate
        ) 
            where TEntity : class
        {
            return _context.Set<TEntity>().Where(predicate);
        }

        public async Task DeleteAsync<TEntity>(TEntity entity)
            where TEntity : class
        {
            _context.Remove(entity);

            await _context.SaveChangesAsync();
        }
    }
}
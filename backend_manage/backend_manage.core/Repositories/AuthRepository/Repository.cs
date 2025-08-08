using System.Linq.Expressions;
using backend_manage.core.Data;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Repositories.AuthRepository
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<T> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            // Kiểm tra xem entity đã được track hay chưa
            var existingEntry = _context.Entry(entity);
            if (existingEntry.State != EntityState.Detached)
            {
                existingEntry.State = EntityState.Detached;
            }
            
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(object id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
        
        public async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

    }
}
using System.Linq.Expressions;
using backend_manage.core.Entities;

namespace backend_manage.core.Repositories.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(object id);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(object id);
        IQueryable<T> GetQueryable();
        Task<T?> GetByConditionAsync(Expression<Func<T, bool>> predicate);

    }
} 
using Bank.DB.Entities.Base;
using System.Linq.Expressions;

namespace Bank.DB.Repositories.Base;

public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    IQueryable<TEntity> Query();
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
}

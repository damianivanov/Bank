using Bank.DB.Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Bank.DB.Repositories.Base;

public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly AppDbContext DbContext;

    public Repository(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public IQueryable<TEntity> Query()
    {
        return DbContext.Set<TEntity>();
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Query().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }
}

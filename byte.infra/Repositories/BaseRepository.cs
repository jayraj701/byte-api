using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Byte.Infra.Repositories;

public abstract class BaseRepository<T>(AppDbContext context) : IRepository<T> where T : AuditableEntity
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking().ToListAsync(ct);

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await DbSet.FindAsync([id], ct);
        if (entity is not null)
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync(ct);
        }
    }
}

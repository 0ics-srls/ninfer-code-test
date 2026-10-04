using Microsoft.EntityFrameworkCore;

namespace MyApp.Infrastructure.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task<T> UpdateAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

internal sealed class Repository<T>(MyApp.Infrastructure.Persistence.AppDbContext db) : IRepository<T>
    where T : class
{
    public IQueryable<T> Query() => db.Set<T>().AsNoTracking();

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await db.Set<T>().AsNoTracking().ToListAsync(ct);

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id, ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await db.Set<T>().AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        db.Set<T>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Set<T>().FindAsync([id], ct);
        if (entity is null) return false;
        db.Set<T>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

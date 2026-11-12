using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Data;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> Query(bool tracking = false);
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}

// One reusable repository for every entity that has Id, audit fields and soft delete
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    // Read-only by default (faster). Pass true when you plan to edit the results.
    public IQueryable<T> Query(bool tracking = false) => tracking ? _set : _set.AsNoTracking();

    public Task<T?> GetByIdAsync(int id) => _set.FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAsync(T entity) => await _set.AddAsync(entity);

    // Soft delete: only flip the IsDeleted flag. Nothing is really deleted, so EF's own
    // delete and cascade logic must not run (it refuses while related rows are loaded).
    // SaveChanges turns this into an UPDATE and writes the "Delete" audit row.
    public void Remove(T entity)
    {
        var entry = _db.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _set.Attach(entity);
        }

        entity.IsDeleted = true;
        entry.Property(e => e.IsDeleted).IsModified = true;
    }

    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
}




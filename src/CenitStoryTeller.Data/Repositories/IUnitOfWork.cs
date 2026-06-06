using System.Threading;
using System.Threading.Tasks;

namespace CenitStoryTeller.Data.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NovelaDbContext _db;
    public UnitOfWork(NovelaDbContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

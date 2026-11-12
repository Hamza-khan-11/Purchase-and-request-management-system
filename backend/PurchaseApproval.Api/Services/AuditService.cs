using PurchaseApproval.Api.Data;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityName, int? entityId,
        string? oldValues = null, string? newValues = null);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(int? userId, string action, string entityName, int? entityId,
        string? oldValues = null, string? newValues = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Data;

// Second half of AppDbContext: fills audit fields, turns deletes into soft deletes, writes AuditLogs
public partial class AppDbContext
{
    private static readonly JsonSerializerOptions AuditJson = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Never written to the audit log (sensitive, or just noise)
    private static readonly HashSet<string> HiddenFromAudit = new()
    {
        "Id", "PasswordHash", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
    };

    private sealed record PendingAudit(BaseEntity Entity, string Action, string? OldValues, string? NewValues);

    private static string ToJson(Dictionary<string, object?> values) =>
        JsonSerializer.Serialize(values, AuditJson);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var pending = new List<PendingAudit>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
        {
            // Never really delete a row: mark it as deleted instead
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }

            if (entry.State == EntityState.Added)
            {
                // Keep an explicit CreatedAt (the seeder sets old dates on purpose)
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy ??= userId;

                if (userId != null)
                {
                    var created = entry.Properties
                        .Where(p => !HiddenFromAudit.Contains(p.Metadata.Name) && p.CurrentValue != null)
                        .ToDictionary(p => p.Metadata.Name, p => (object?)p.CurrentValue);
                    pending.Add(new PendingAudit(entry.Entity, "Create", null, ToJson(created)));
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // CreatedAt and CreatedBy must never change after the first save
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;

                if (userId != null)
                {
                    var changed = entry.Properties
                        .Where(p => p.IsModified
                                    && !HiddenFromAudit.Contains(p.Metadata.Name)
                                    && !Equals(p.OriginalValue, p.CurrentValue))
                        .ToList();

                    if (changed.Count > 0)
                    {
                        var oldValues = changed.ToDictionary(p => p.Metadata.Name, p => (object?)p.OriginalValue);
                        var newValues = changed.ToDictionary(p => p.Metadata.Name, p => (object?)p.CurrentValue);
                        var action = newValues.TryGetValue("IsDeleted", out var flag) && flag is true ? "Delete" : "Update";
                        pending.Add(new PendingAudit(entry.Entity, action, ToJson(oldValues), ToJson(newValues)));
                    }
                }
            }
        }

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // Second save: the new rows now have real Ids, so the audit rows can point at them
        if (pending.Count > 0)
        {
            foreach (var p in pending)
            {
                AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = p.Action,
                    EntityName = p.Entity.GetType().Name,
                    EntityId = p.Entity.Id,
                    OldValues = p.OldValues,
                    NewValues = p.NewValues,
                    Timestamp = now
                });
            }
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        return result;
    }

    // Same behaviour if some code calls the non-async SaveChanges()
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
}

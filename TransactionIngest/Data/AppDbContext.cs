using TransactionIngest.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace TransactionIngest.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<StatusType> StatusTypes => Set<StatusType>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StatusType>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.HasData(
                new StatusType { Id = (int)StatusTypeValues.Active, Name = "Active" },
                new StatusType { Id = (int)StatusTypeValues.Revoked, Name = "Revoked" },
                new StatusType { Id = (int)StatusTypeValues.Finalized, Name = "Finalized" }
            );
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = base.SaveChanges();
        OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is Transaction && (e.State == EntityState.Added || e.State == EntityState.Modified)))
        {
            var auditEntry = new AuditEntry
            {
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Username = "ConsoleAppUser", 
                Timestamp = DateTime.Now
            };

            foreach (var property in entry.Properties)
            {
                if (property.IsModified || entry.State == EntityState.Added)
                {
                    auditEntry.ChangesList.Add(new {
                        PropertyName = property.Metadata.Name,
                        OldValue = property.OriginalValue,
                        NewValue = property.CurrentValue
                    });
                }
            }
            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }

    private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        foreach (var auditEntry in auditEntries)
        {
            AuditLogs.Add(new AuditLog
            {
                EntityName = auditEntry.EntityName,
                Action = auditEntry.Action,
                Timestamp = auditEntry.Timestamp,
                Username = auditEntry.Username,
                Changes = JsonSerializer.Serialize(auditEntry.ChangesList),
            });
        }

        base.SaveChanges();
    }
}


public class AuditEntry
{
    public string EntityName { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string Username { get; set; }
    public List<object> ChangesList { get; set; } = new List<object>();
}

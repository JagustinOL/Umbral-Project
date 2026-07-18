using Microsoft.EntityFrameworkCore;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;

namespace ScoringAudit.Infrastructure.Persistence;

public sealed class ScoringAuditDbContext : DbContext
{
    public ScoringAuditDbContext(DbContextOptions<ScoringAuditDbContext> options) : base(options) { }

    public DbSet<TeamLedger> TeamLedgers => Set<TeamLedger>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoringAuditDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

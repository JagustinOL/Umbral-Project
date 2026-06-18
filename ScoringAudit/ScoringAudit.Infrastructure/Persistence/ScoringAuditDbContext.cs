using Microsoft.EntityFrameworkCore;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;

namespace ScoringAudit.Infrastructure.Persistence;

public sealed class ScoringAuditDbContext : DbContext
{
    public ScoringAuditDbContext(DbContextOptions<ScoringAuditDbContext> options) : base(options) { }

    public DbSet<TeamLedger> TeamLedgers => Set<TeamLedger>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoringAuditDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

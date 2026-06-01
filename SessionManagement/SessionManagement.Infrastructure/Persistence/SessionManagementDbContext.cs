using Microsoft.EntityFrameworkCore;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Infrastructure.Persistence;

public sealed class SessionManagementDbContext : DbContext
{
    public SessionManagementDbContext(DbContextOptions<SessionManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<EvidenceSubmission> EvidenceSubmissions => Set<EvidenceSubmission>();
    public DbSet<ReleasedHint> ReleasedHints => Set<ReleasedHint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SessionManagementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}


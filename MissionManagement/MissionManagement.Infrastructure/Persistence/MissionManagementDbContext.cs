using Microsoft.EntityFrameworkCore;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;

namespace MissionManagement.Infrastructure.Persistence;

public sealed class MissionManagementDbContext : DbContext
{
    public MissionManagementDbContext(DbContextOptions<MissionManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionNode> MissionNodes => Set<MissionNode>();
    public DbSet<Hint> Hints => Set<Hint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MissionManagementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}


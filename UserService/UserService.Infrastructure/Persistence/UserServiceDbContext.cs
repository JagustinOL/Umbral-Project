using Microsoft.EntityFrameworkCore;
using UserService.Domain.Aggregates;

namespace UserService.Infrastructure.Persistence;

public sealed class UserServiceDbContext : DbContext
{
    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserServiceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

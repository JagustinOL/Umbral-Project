using Microsoft.EntityFrameworkCore;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly UserServiceDbContext _dbContext;

    public UserRepository(UserServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email.Trim(), cancellationToken);

    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var tracked = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken);
        if (tracked is null)
        {
            await _dbContext.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            _dbContext.Entry(tracked).CurrentValues.SetValues(new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.Status
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsActiveOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AsNoTracking().AnyAsync(
            x => x.Id == operatorId
                 && x.Role == UserRole.Operator
                 && x.Status == UserStatus.Active,
            cancellationToken);
}

using NAgent.Domain.Entities;
using NAgent.Domain.Repositories;
using NAgent.Infrastructure.Persistence;

namespace NAgent.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Queryable<User>().InSingleAsync(id);
    }

    public async Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Queryable<User>().ToListAsync();
        return users.AsReadOnly();
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Queryable<User>().FirstAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Queryable<User>().FirstAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var count = await _context.Queryable<User>().CountAsync(u => u.Username == username);
        return count > 0;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var count = await _context.Queryable<User>().CountAsync(u => u.Email == email);
        return count > 0;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Insertable(user).ExecuteCommandAsync();
    }

    public void Update(User user)
    {
        _context.Updateable(user).ExecuteCommand();
    }

    public void Delete(User user)
    {
        _context.Deleteable(user).ExecuteCommand();
    }
}

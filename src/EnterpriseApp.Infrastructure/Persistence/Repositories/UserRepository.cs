using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseApp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly EnterpriseDbContext _context;

    public UserRepository(EnterpriseDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == normalized, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default)
    {
        var uNorm = username.Trim().ToLowerInvariant();
        var eNorm = email.Trim().ToLowerInvariant();
        return await _context.Users.AnyAsync(u => u.Username.ToLower() == uNorm || u.Email == eNorm, ct);
    }
}

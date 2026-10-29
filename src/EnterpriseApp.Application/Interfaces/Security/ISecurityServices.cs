using EnterpriseApp.Domain.Entities;

namespace EnterpriseApp.Application.Interfaces.Security;

public interface ITokenService
{
    string GenerateToken(User user);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

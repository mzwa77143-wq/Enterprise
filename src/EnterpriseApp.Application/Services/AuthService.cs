using EnterpriseApp.Application.DTOs.Auth;
using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Application.Interfaces.Security;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using FluentValidation;
using ValidationException = EnterpriseApp.Domain.Exceptions.ValidationException;

namespace EnterpriseApp.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, ct);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid username or password.");
        }

        var token = _tokenService.GenerateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(8)
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        if (await _userRepository.ExistsAsync(request.Username, request.Email, ct))
        {
            throw new DomainException("A user with this username or email already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.Username, request.Email, passwordHash, request.Role);
        var created = await _userRepository.AddAsync(user, ct);

        var token = _tokenService.GenerateToken(created);
        return new AuthResponseDto
        {
            Token = token,
            Username = created.Username,
            Email = created.Email,
            Role = created.Role,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(8)
        };
    }
}

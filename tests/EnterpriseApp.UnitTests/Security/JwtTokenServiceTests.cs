using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EnterpriseApp.UnitTests.Security;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldIncludeUserClaimsAndValidSignature()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "SuperSecretKeyForTestingEnterpriseJwtTokens1234567890!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpirationHours"] = "4"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var tokenService = new JwtTokenService(config);

        var user = new User("alice_admin", "alice@enterprise.com", "dummy_hash", "Admin");

        // Act
        var tokenString = tokenService.GenerateToken(user);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(tokenString).Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(tokenString);
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");

        // Verify claims
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id.ToString());
        jwtToken.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value.Should().Be("Admin");
        jwtToken.Claims.First(c => c.Type == "email" || c.Type == ClaimTypes.Email).Value.Should().Be("alice@enterprise.com");
    }

    [Fact]
    public void PasswordHasher_HashPassword_ShouldGenerateUniqueSaltForSameInput()
    {
        // Arrange
        var hasher = new PasswordHasher();
        const string password = "Password@123";

        // Act
        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2, "distinct salts must be generated on every hash call");
    }

    [Theory]
    [InlineData("Admin@123456")]
    [InlineData("VerySecureComplexPassword!#$99")]
    public void PasswordHasher_VerifyPassword_CorrectPassword_ShouldReturnTrue(string password)
    {
        // Arrange
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword(password);

        // Act
        var isValid = hasher.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_IncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("CorrectPassword123");

        // Act
        var isValid = hasher.VerifyPassword("WrongPassword456", hash);

        // Assert
        isValid.Should().BeFalse();
    }
}

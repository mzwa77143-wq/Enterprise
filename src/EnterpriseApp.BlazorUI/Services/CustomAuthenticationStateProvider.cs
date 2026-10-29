using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EnterpriseApp.BlazorUI.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser;
    private string? _token;

    public CustomAuthenticationStateProvider()
    {
        _currentUser = _anonymous;
    }

    public string? Token => _token;
    public bool IsAuthenticated => _currentUser.Identity?.IsAuthenticated ?? false;
    public string? Username => _currentUser.FindFirst(ClaimTypes.Name)?.Value ?? _currentUser.FindFirst("unique_name")?.Value;
    public string? Email => _currentUser.FindFirst(ClaimTypes.Email)?.Value;
    public string? Role => _currentUser.FindFirst(ClaimTypes.Role)?.Value;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public void MarkUserAsAuthenticated(string token)
    {
        _token = token;
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void MarkUserAsLoggedOut()
    {
        _token = null;
        _currentUser = _anonymous;

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var claims = new List<Claim>();

            foreach (var claim in token.Claims)
            {
                // Normalize standard claims
                if (claim.Type == "sub" || claim.Type == JwtRegisteredClaimNames.Sub)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                }
                else if (claim.Type == "unique_name" || claim.Type == JwtRegisteredClaimNames.UniqueName || claim.Type == "name")
                {
                    claims.Add(new Claim(ClaimTypes.Name, claim.Value));
                }
                else if (claim.Type == "email" || claim.Type == JwtRegisteredClaimNames.Email)
                {
                    claims.Add(new Claim(ClaimTypes.Email, claim.Value));
                }
                else if (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                {
                    claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                }
                else
                {
                    claims.Add(claim);
                }
            }

            return claims;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }
}

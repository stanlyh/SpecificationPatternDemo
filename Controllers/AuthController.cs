using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpecificationPatternDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;

    public AuthController(JwtOptions jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    // Demo login endpoint. Accepts username and optional role.
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(JwtRegisteredClaimNames.Sub, request.Username)
        };

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, request.Role));
        }

        var token = BuildToken(claims);

        return Ok(new { token, username = request.Username, role = request.Role });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        // In a real app validate refresh token; here we simply re-issue a new token for demonstration.
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(request.Token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtOptions.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
                ValidateLifetime = false // allow expired tokens for refresh demo only
            }, out var validatedToken);

            var username = principal.Identity?.Name ?? "";
            var role = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            if (!string.IsNullOrWhiteSpace(role)) claims.Add(new Claim(ClaimTypes.Role, role));

            var newToken = BuildToken(claims);
            return Ok(new { token = newToken, username, role });
        }
        catch
        {
            return BadRequest("Invalid token");
        }
    }

    private string BuildToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Username, string? Role);
public record RefreshRequest(string Token);

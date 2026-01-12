using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace SpecificationPatternDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;
    private readonly ApplicationDbContext _db;

    public AuthController(JwtOptions jwtOptions, ApplicationDbContext db)
    {
        _jwtOptions = jwtOptions;
        _db = db;
    }

    private static string Hash(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    // Demo login endpoint. Accepts username and optional role.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, request.Username),
            new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, request.Username)
        };

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, request.Role));
        }

        var token = BuildToken(claims);

        // create refresh token value and persist only hash
        var rawRefresh = Guid.NewGuid().ToString("N");
        var refreshHash = Hash(rawRefresh);

        var refresh = new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = request.Username,
            CreatedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return Ok(new { token, refreshToken = rawRefresh, username = request.Username, role = request.Role });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        // request should contain refreshToken raw value
        var hash = Hash(request.Token);
        var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (refreshToken is null) return BadRequest("Invalid refresh token");
        if (refreshToken.IsRevoked) return BadRequest("Refresh token revoked");
        if (refreshToken.IsExpired) return BadRequest("Refresh token expired");

        // issue new jwt and new refresh token (rotate)
        var username = refreshToken.UserId;
        var claims = new List<System.Security.Claims.Claim> { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) };
        var newJwt = BuildToken(claims);

        // revoke old refresh token and create a new one
        refreshToken.RevokedAt = DateTime.UtcNow;

        var newRawRefresh = Guid.NewGuid().ToString("N");
        var newRefresh = new RefreshToken
        {
            TokenHash = Hash(newRawRefresh),
            UserId = username,
            CreatedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(newRefresh);
        await _db.SaveChangesAsync();

        return Ok(new { token = newJwt, refreshToken = newRawRefresh, username });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
    {
        var hash = Hash(request.RefreshToken);
        var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (refreshToken is null) return NotFound();

        refreshToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private string BuildToken(IEnumerable<System.Security.Claims.Claim> claims)
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
public record RevokeRequest(string RefreshToken);

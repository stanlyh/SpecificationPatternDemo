namespace SpecificationPatternDemo;

public class RefreshToken
{
    public int Id { get; set; }
    // Store only the hash of the refresh token
    public string TokenHash { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime Expires { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => RevokedAt.HasValue;
}

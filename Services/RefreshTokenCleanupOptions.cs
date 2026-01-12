namespace SpecificationPatternDemo.Services;

public class RefreshTokenCleanupOptions
{
    // Interval in minutes between cleanup runs
    public int IntervalMinutes { get; set; } = 60;

    // Retention days: remove tokens expired or revoked older than this
    public int RetentionDays { get; set; } = 7;
}
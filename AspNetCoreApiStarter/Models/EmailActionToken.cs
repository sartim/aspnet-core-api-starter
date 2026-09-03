namespace AspNetCoreApiStarter.Models;

public sealed class EmailActionToken : Base
{
    public required Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Purpose { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApiStarter.Models;

public class RevokedAccessToken
{
    [Key]
    public Guid Id { get; set; }

    public required string JwtId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; }
}

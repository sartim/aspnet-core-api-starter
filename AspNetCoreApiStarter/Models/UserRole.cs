using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetCoreApiStarter.Models;

public class UserRole
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(Role))]
    public Guid RoleId { get; set; }

    public required User User { get; set; }
    public required Role Role { get; set; }
}

using System;
namespace AspNetCoreApiStarter.Models

{
    public class Role : Base
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

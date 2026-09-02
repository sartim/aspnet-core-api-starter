using System;
namespace AspNetCoreApiStarter.Models
{
    public class User : Base
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required int Phone { get; set; }
        public required string Password { get; set; }
        public bool IsActive { get; set; } = false;
    }
}

using System;
namespace AspNetCoreApiStarter.Models
{
    public class Permission : Base
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}


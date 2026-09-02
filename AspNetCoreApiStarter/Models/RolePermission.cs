using System;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetCoreApiStarter.Models
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Role")]
        public Guid RoleId { get; set; }

        [ForeignKey("Permission")]
        public Guid PermissionId { get; set; }

        public required Role Role { get; set; }
        public required Permission Permission { get; set; }
    }
}


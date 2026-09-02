using System;
using AspNetCoreApiStarter.Models;
using Microsoft.EntityFrameworkCore;


namespace AspNetCoreApiStarter.Data;


public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermission { get; set; }
}


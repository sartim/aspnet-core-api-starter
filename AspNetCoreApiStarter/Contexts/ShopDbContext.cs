using System;
using AspNetCoreApiStarter.Models;
using Microsoft.EntityFrameworkCore;


namespace AspNetCoreApiStarter.Data;


public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermission { get; set; }
}


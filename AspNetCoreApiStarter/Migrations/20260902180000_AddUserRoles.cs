using Microsoft.EntityFrameworkCore.Migrations;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AspNetCoreApiStarter.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260902180000_AddUserRoles")]
public partial class AddUserRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => x.Id);
                table.ForeignKey("FK_UserRoles_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_UserRoles_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_UserRoles_RoleId", "UserRoles", "RoleId");
        migrationBuilder.CreateIndex("IX_UserRoles_UserId_RoleId", "UserRoles", new[] { "UserId", "RoleId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserRoles");
    }
}

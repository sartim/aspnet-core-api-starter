using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreApiStarter.Migrations;

public partial class AddAuthenticationSecurityControls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("FailedLoginAttempts", "Users", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>("LastLoginAt", "Users", nullable: true);
        migrationBuilder.AddColumn<DateTime>("LockoutEnd", "Users", nullable: true);

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey("FK_RefreshTokens_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RevokedAccessTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JwtId = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RevokedAccessTokens", x => x.Id));

        migrationBuilder.CreateIndex("IX_RefreshTokens_TokenHash", "RefreshTokens", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_RefreshTokens_UserId", "RefreshTokens", "UserId");
        migrationBuilder.CreateIndex("IX_RevokedAccessTokens_JwtId", "RevokedAccessTokens", "JwtId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("RefreshTokens");
        migrationBuilder.DropTable("RevokedAccessTokens");
        migrationBuilder.DropColumn("FailedLoginAttempts", "Users");
        migrationBuilder.DropColumn("LastLoginAt", "Users");
        migrationBuilder.DropColumn("LockoutEnd", "Users");
    }
}

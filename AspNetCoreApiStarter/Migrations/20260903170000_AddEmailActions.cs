using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreApiStarter.Migrations;

public partial class AddEmailActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("EmailVerifiedAt", "Users", nullable: true);
        migrationBuilder.CreateTable(
            name: "EmailActionTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Purpose = table.Column<string>(type: "text", nullable: false),
                TokenHash = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailActionTokens", x => x.Id);
                table.ForeignKey("FK_EmailActionTokens_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_EmailActionTokens_TokenHash", "EmailActionTokens", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_EmailActionTokens_UserId_Purpose_UsedAt", "EmailActionTokens", new[] { "UserId", "Purpose", "UsedAt" });
        migrationBuilder.CreateIndex("IX_EmailActionTokens_UserId", "EmailActionTokens", "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("EmailActionTokens");
        migrationBuilder.DropColumn("EmailVerifiedAt", "Users");
    }
}

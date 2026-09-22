using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreApiStarter.Migrations
{
    /// <inheritdoc />
    public partial class DotNet10Upgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailActionTokens_UserId",
                table: "EmailActionTokens");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmailActionTokens_UserId",
                table: "EmailActionTokens",
                column: "UserId");
        }
    }
}

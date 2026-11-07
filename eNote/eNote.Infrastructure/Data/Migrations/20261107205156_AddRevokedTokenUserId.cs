using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRevokedTokenUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [RevokedToken];");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RevokedToken",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RevokedToken_UserId",
                table: "RevokedToken",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RevokedToken_AspNetUsers_UserId",
                table: "RevokedToken",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RevokedToken_AspNetUsers_UserId",
                table: "RevokedToken");

            migrationBuilder.DropIndex(
                name: "IX_RevokedToken_UserId",
                table: "RevokedToken");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RevokedToken");
        }
    }
}

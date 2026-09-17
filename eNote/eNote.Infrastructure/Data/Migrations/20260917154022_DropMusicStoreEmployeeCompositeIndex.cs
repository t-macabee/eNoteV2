using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropMusicStoreEmployeeCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MusicStoreEmployee_MusicStoreId_AppUserId",
                table: "MusicStoreEmployee");

            migrationBuilder.CreateIndex(
                name: "IX_MusicStoreEmployee_MusicStoreId",
                table: "MusicStoreEmployee",
                column: "MusicStoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MusicStoreEmployee_MusicStoreId",
                table: "MusicStoreEmployee");

            migrationBuilder.CreateIndex(
                name: "IX_MusicStoreEmployee_MusicStoreId_AppUserId",
                table: "MusicStoreEmployee",
                columns: new[] { "MusicStoreId", "AppUserId" },
                unique: true);
        }
    }
}

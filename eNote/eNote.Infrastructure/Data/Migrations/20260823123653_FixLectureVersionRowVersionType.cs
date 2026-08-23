using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixLectureVersionRowVersionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server won't ALTER COLUMN ... to timestamp/rowversion in place
            // (Error 4927), so drop and re-add instead. Lecture had no rows at the
            // time of this migration, so there is no data to preserve.
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Lecture");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Lecture",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Lecture");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Lecture",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L);
        }
    }
}

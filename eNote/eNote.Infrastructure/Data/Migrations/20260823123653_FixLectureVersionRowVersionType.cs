using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{

    public partial class FixLectureVersionRowVersionType : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {

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

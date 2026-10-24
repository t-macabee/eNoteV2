using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndividualLectures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "Lecture",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_StudentId",
                table: "Lecture",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lecture_Student_StudentId",
                table: "Lecture",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lecture_Student_StudentId",
                table: "Lecture");

            migrationBuilder.DropIndex(
                name: "IX_Lecture_StudentId",
                table: "Lecture");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Lecture");
        }
    }
}

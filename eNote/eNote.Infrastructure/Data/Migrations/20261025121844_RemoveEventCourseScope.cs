using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEventCourseScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_Course_CourseId",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Event_Instructor_InstructorId",
                table: "Event");

            migrationBuilder.DropIndex(
                name: "IX_Event_CourseId",
                table: "Event");

            migrationBuilder.DropIndex(
                name: "IX_Event_InstructorId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Event");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Event",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Event",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_CourseId",
                table: "Event",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_InstructorId",
                table: "Event",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Course_CourseId",
                table: "Event",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Instructor_InstructorId",
                table: "Event",
                column: "InstructorId",
                principalTable: "Instructor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

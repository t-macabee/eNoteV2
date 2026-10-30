using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Notification",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_CourseId",
                table: "Notification",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Course_CourseId",
                table: "Notification",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Course_CourseId",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Notification_CourseId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Notification");
        }
    }
}

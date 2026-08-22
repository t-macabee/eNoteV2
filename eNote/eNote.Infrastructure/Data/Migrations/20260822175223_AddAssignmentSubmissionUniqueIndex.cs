using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentSubmissionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmission_AssignmentId",
                table: "AssignmentSubmission");

            migrationBuilder.CreateIndex(
                name: DbConstraintNames.AssignmentSubmissionAssignmentIdStudentIdUniqueIndex,
                table: "AssignmentSubmission",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: DbConstraintNames.AssignmentSubmissionAssignmentIdStudentIdUniqueIndex,
                table: "AssignmentSubmission");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_AssignmentId",
                table: "AssignmentSubmission",
                column: "AssignmentId");
        }
    }
}

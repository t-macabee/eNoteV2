using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OneOpenPaymentAttemptPerObligation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_RentalPayment_InstrumentRentalId_OpenAttempt",
                table: "RentalPayment",
                column: "InstrumentRentalId",
                unique: true,
                filter: "\"Status\" IN (1, 3)");

            migrationBuilder.CreateIndex(
                name: "UX_CoursePayment_EnrollmentId_OpenAttempt",
                table: "CoursePayment",
                column: "EnrollmentId",
                unique: true,
                filter: "\"Status\" IN (1, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_RentalPayment_InstrumentRentalId_OpenAttempt",
                table: "RentalPayment");

            migrationBuilder.DropIndex(
                name: "UX_CoursePayment_EnrollmentId_OpenAttempt",
                table: "CoursePayment");
        }
    }
}

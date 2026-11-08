using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledAt",
                table: "InstrumentRental",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanceledById",
                table: "InstrumentRental",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "InstrumentRental",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("UPDATE InstrumentRental SET DecisionNote = Note WHERE RentalStatus = 2 OR RejectedAt IS NOT NULL;");

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceledAt",
                table: "Enrollment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanceledById",
                table: "Enrollment",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Enrollment",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentRental_CanceledById",
                table: "InstrumentRental",
                column: "CanceledById");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_CanceledById",
                table: "Enrollment",
                column: "CanceledById");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_AspNetUsers_CanceledById",
                table: "Enrollment",
                column: "CanceledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_CanceledById",
                table: "InstrumentRental",
                column: "CanceledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_AspNetUsers_CanceledById",
                table: "Enrollment");

            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_CanceledById",
                table: "InstrumentRental");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentRental_CanceledById",
                table: "InstrumentRental");

            migrationBuilder.DropIndex(
                name: "IX_Enrollment_CanceledById",
                table: "Enrollment");

            migrationBuilder.DropColumn(
                name: "CanceledAt",
                table: "InstrumentRental");

            migrationBuilder.DropColumn(
                name: "CanceledById",
                table: "InstrumentRental");

            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "InstrumentRental");

            migrationBuilder.DropColumn(
                name: "CanceledAt",
                table: "Enrollment");

            migrationBuilder.DropColumn(
                name: "CanceledById",
                table: "Enrollment");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Enrollment");
        }
    }
}

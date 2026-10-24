using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LectureCancellationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Lecture",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Lecture",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledById",
                table: "Lecture",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_CancelledById",
                table: "Lecture",
                column: "CancelledById");

            migrationBuilder.AddForeignKey(
                name: "FK_Lecture_AspNetUsers_CancelledById",
                table: "Lecture",
                column: "CancelledById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lecture_AspNetUsers_CancelledById",
                table: "Lecture");

            migrationBuilder.DropIndex(
                name: "IX_Lecture_CancelledById",
                table: "Lecture");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Lecture");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Lecture");

            migrationBuilder.DropColumn(
                name: "CancelledById",
                table: "Lecture");
        }
    }
}

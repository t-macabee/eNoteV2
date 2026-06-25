using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class RentalOutboxAndInstrumentFees : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RentalNotificationOutbox",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedById = table.Column<int>(type: "int", nullable: true),
                UpdatedById = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RentalNotificationOutbox", x => x.Id);
            });

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 1,
            column: "MonthlyFee",
            value: 45m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 2,
            column: "MonthlyFee",
            value: 35m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 3,
            column: "MonthlyFee",
            value: 55m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 4,
            column: "MonthlyFee",
            value: 65m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 5,
            column: "MonthlyFee",
            value: 15m);

        migrationBuilder.CreateIndex(
            name: "IX_RentalNotificationOutbox_PublishedAt",
            table: "RentalNotificationOutbox",
            column: "PublishedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RentalNotificationOutbox");

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 1,
            column: "MonthlyFee",
            value: 0m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 2,
            column: "MonthlyFee",
            value: 0m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 3,
            column: "MonthlyFee",
            value: 0m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 4,
            column: "MonthlyFee",
            value: 0m);

        migrationBuilder.UpdateData(
            table: "InstrumentType",
            keyColumn: "Id",
            keyValue: 5,
            column: "MonthlyFee",
            value: 0m);
    }
}

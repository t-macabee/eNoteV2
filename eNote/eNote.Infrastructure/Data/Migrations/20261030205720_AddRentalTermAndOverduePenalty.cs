using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    public partial class AddRentalTermAndOverduePenalty : Migration
    {
            protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "InstrumentRental",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OverduePenaltyMultiplier",
                table: "InstrumentRental",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE [InstrumentRental] SET [DueAt] = DATEADD(day, 30, [PickedUpAt]) WHERE [PickedUpAt] IS NOT NULL;");
        }

            protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "InstrumentRental");

            migrationBuilder.DropColumn(
                name: "OverduePenaltyMultiplier",
                table: "InstrumentRental");
        }
    }
}

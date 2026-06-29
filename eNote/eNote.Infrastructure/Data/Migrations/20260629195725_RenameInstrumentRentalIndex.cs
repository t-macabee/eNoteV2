using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameInstrumentRentalIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_InstrumentRental_InstrumentId",
                table: "InstrumentRental",
                newName: "UX_InstrumentRental_InstrumentId_ActiveOrApproved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_InstrumentRental_InstrumentId_ActiveOrApproved",
                table: "InstrumentRental",
                newName: "IX_InstrumentRental_InstrumentId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentTypeUniqueNameAndMinimumFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "InstrumentType",
                keyColumn: "Id",
                keyValue: 5,
                column: "MonthlyFee",
                value: 30m);

            migrationBuilder.CreateIndex(
                name: "UX_InstrumentType_Type",
                table: "InstrumentType",
                column: "Type",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_InstrumentType_Type",
                table: "InstrumentType");

            migrationBuilder.UpdateData(
                table: "InstrumentType",
                keyColumn: "Id",
                keyValue: 5,
                column: "MonthlyFee",
                value: 15m);
        }
    }
}

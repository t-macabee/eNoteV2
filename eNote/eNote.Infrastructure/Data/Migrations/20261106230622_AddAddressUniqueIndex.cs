using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Address_CityId",
                table: "Address");

            migrationBuilder.CreateIndex(
                name: "UX_Address_CityId_Street_Number",
                table: "Address",
                columns: new[] { "CityId", "Street", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Address_CityId_Street_Number",
                table: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CityId",
                table: "Address",
                column: "CityId");
        }
    }
}

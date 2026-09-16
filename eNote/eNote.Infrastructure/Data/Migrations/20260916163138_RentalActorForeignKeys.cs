using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RentalActorForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The actor columns were bare ints before this migration; clear any id that no longer resolves so the new constraints can be added.
            migrationBuilder.Sql("UPDATE [InstrumentRental] SET [ApprovedById] = NULL WHERE [ApprovedById] IS NOT NULL AND [ApprovedById] NOT IN (SELECT [Id] FROM [AspNetUsers]);");
            migrationBuilder.Sql("UPDATE [InstrumentRental] SET [RejectedById] = NULL WHERE [RejectedById] IS NOT NULL AND [RejectedById] NOT IN (SELECT [Id] FROM [AspNetUsers]);");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentRental_ApprovedById",
                table: "InstrumentRental",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentRental_RejectedById",
                table: "InstrumentRental",
                column: "RejectedById");

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_ApprovedById",
                table: "InstrumentRental",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_RejectedById",
                table: "InstrumentRental",
                column: "RejectedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_ApprovedById",
                table: "InstrumentRental");

            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_RejectedById",
                table: "InstrumentRental");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentRental_ApprovedById",
                table: "InstrumentRental");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentRental_RejectedById",
                table: "InstrumentRental");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_Name",
                table: "City",
                column: "Name",
                unique: true);

            migrationBuilder.DropColumn(
                name: "City",
                table: "Address");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Address",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.InsertData(
                table: "City",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Sarajevo" },
                    { 2, "Mostar" },
                    { 3, "Banja Luka" },
                    { 4, "Tuzla" },
                    { 5, "Zenica" }
                });

            migrationBuilder.Sql(@"
                UPDATE Address SET CityId = 1 WHERE CityId = 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CityId",
                table: "Address",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_City_CityId",
                table: "Address",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Reseed addresses with CityId (EF seed data updated)
            migrationBuilder.UpdateData(
                table: "Address",
                keyColumn: "Id",
                keyValue: 1,
                column: "CityId",
                value: 1);
            migrationBuilder.UpdateData(
                table: "Address",
                keyColumn: "Id",
                keyValue: 2,
                column: "CityId",
                value: 1);
            migrationBuilder.UpdateData(
                table: "Address",
                keyColumn: "Id",
                keyValue: 3,
                column: "CityId",
                value: 1);
            migrationBuilder.UpdateData(
                table: "Address",
                keyColumn: "Id",
                keyValue: 4,
                column: "CityId",
                value: 1);
            migrationBuilder.UpdateData(
                table: "Address",
                keyColumn: "Id",
                keyValue: 5,
                column: "CityId",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Address_City_CityId",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_CityId",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Address");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Address",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropTable(
                name: "City");
        }
    }
}

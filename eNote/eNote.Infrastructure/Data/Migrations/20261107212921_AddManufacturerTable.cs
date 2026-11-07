using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Manufacturer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturer", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Manufacturer_Name",
                table: "Manufacturer",
                column: "Name",
                unique: true);

            migrationBuilder.InsertData(
                table: "Manufacturer",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Bach" },
                    { 2, "Boss" },
                    { 3, "Fender" },
                    { 4, "Gibson" },
                    { 5, "Ibanez" },
                    { 6, "Jackson" },
                    { 7, "Korg" },
                    { 8, "Ludwig" },
                    { 9, "Marshall" },
                    { 10, "Martin" },
                    { 11, "Music Man" },
                    { 12, "Orange" },
                    { 13, "Pearl" },
                    { 14, "PRS" },
                    { 15, "Roland" },
                    { 16, "Seagull" },
                    { 17, "Tama" },
                    { 18, "Taylor" },
                    { 19, "Vox" },
                    { 20, "Yamaha" }
                });

            migrationBuilder.Sql(
                """
                UPDATE [Instrument]
                SET [Manufacturer] = N'Nepoznato'
                WHERE LTRIM(RTRIM([Manufacturer])) = N'';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [Manufacturer] ([Name])
                SELECT MIN(LTRIM(RTRIM(i.[Manufacturer])))
                FROM [Instrument] i
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Manufacturer] m
                    WHERE UPPER(LTRIM(RTRIM(m.[Name]))) = UPPER(LTRIM(RTRIM(i.[Manufacturer])))
                )
                GROUP BY UPPER(LTRIM(RTRIM(i.[Manufacturer])));
                """);

            migrationBuilder.Sql("ALTER TABLE [Instrument] ADD [ManufacturerId] int NULL;");

            migrationBuilder.Sql(
                """
                UPDATE i
                SET i.[ManufacturerId] = m.[Id]
                FROM [Instrument] i
                INNER JOIN [Manufacturer] m
                    ON UPPER(LTRIM(RTRIM(m.[Name]))) = UPPER(LTRIM(RTRIM(i.[Manufacturer])));
                """);

            migrationBuilder.Sql("ALTER TABLE [Instrument] ALTER COLUMN [ManufacturerId] int NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Instrument_ManufacturerId",
                table: "Instrument",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instrument_Manufacturer_ManufacturerId",
                table: "Instrument",
                column: "ManufacturerId",
                principalTable: "Manufacturer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Instrument");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Instrument",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE i
                SET i.[Manufacturer] = m.[Name]
                FROM [Instrument] i
                INNER JOIN [Manufacturer] m ON m.[Id] = i.[ManufacturerId];

                ALTER TABLE [Instrument] ALTER COLUMN [Manufacturer] nvarchar(100) NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Instrument_Manufacturer_ManufacturerId",
                table: "Instrument");

            migrationBuilder.DropIndex(
                name: "IX_Instrument_ManufacturerId",
                table: "Instrument");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "Instrument");

            migrationBuilder.DropTable(
                name: "Manufacturer");
        }
    }
}

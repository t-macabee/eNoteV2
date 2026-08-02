using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations;

public partial class ReplaceUserPictureBlobWithPath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Picture",
            table: "AspNetUsers");

        migrationBuilder.AddColumn<string>(
            name: "PicturePath",
            table: "AspNetUsers",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PicturePath",
            table: "AspNetUsers");

        migrationBuilder.AddColumn<byte[]>(
            name: "Picture",
            table: "AspNetUsers",
            type: "bytea",
            nullable: true);
    }
}

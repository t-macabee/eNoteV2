using eNote.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations;

[DbContext(typeof(ENoteContext))]
[Migration("20260802211850_AddNotificationUniqueIndex")]
public partial class AddNotificationUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Notification_UserId_RentalId_CreatedAt",
            table: "Notification",
            columns: new[] { "UserId", "RentalId", "CreatedAt" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Notification_UserId_RentalId_CreatedAt",
            table: "Notification");
    }
}

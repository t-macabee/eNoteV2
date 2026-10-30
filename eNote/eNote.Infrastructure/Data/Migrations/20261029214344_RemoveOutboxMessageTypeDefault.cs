using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOutboxMessageTypeDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                table: "RentalNotificationOutbox",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldDefaultValue: "RentalStatusChanged");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                table: "RentalNotificationOutbox",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "RentalStatusChanged",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoursePaymentRefunds : Migration
    {
        private const string OnePaymentCheckFilter = "(\"RentalPaymentId\" IS NOT NULL AND \"CoursePaymentId\" IS NULL) OR (\"RentalPaymentId\" IS NULL AND \"CoursePaymentId\" IS NOT NULL)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "RentalRefund",
                newName: "PaymentRefund");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRefund_RentalPayment_RentalPaymentId",
                table: "PaymentRefund");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalRefund",
                table: "PaymentRefund");

            migrationBuilder.DropIndex(
                name: "IX_RentalRefund_StripeRefundId",
                table: "PaymentRefund");

            migrationBuilder.DropIndex(
                name: "IX_RentalRefund_RentalPaymentId",
                table: "PaymentRefund");

            migrationBuilder.AlterColumn<int>(
                name: "RentalPaymentId",
                table: "PaymentRefund",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CoursePaymentId",
                table: "PaymentRefund",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentRefund",
                table: "PaymentRefund",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRefund_RentalPayment_RentalPaymentId",
                table: "PaymentRefund",
                column: "RentalPaymentId",
                principalTable: "RentalPayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRefund_CoursePayment_CoursePaymentId",
                table: "PaymentRefund",
                column: "CoursePaymentId",
                principalTable: "CoursePayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefund_RentalPaymentId",
                table: "PaymentRefund",
                column: "RentalPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefund_CoursePaymentId",
                table: "PaymentRefund",
                column: "CoursePaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentRefund_StripeRefundId",
                table: "PaymentRefund",
                column: "StripeRefundId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentRefund_ExactlyOnePayment",
                table: "PaymentRefund",
                sql: OnePaymentCheckFilter);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "CoursePayment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RefundedCents",
                table: "CoursePayment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeRefundId",
                table: "CoursePayment",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "CoursePayment",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "CoursePayment");

            migrationBuilder.DropColumn(
                name: "RefundedCents",
                table: "CoursePayment");

            migrationBuilder.DropColumn(
                name: "StripeRefundId",
                table: "CoursePayment");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CoursePayment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentRefund_ExactlyOnePayment",
                table: "PaymentRefund");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRefund_CoursePayment_CoursePaymentId",
                table: "PaymentRefund");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRefund_CoursePaymentId",
                table: "PaymentRefund");

            migrationBuilder.DropColumn(
                name: "CoursePaymentId",
                table: "PaymentRefund");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRefund_RentalPayment_RentalPaymentId",
                table: "PaymentRefund");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentRefund",
                table: "PaymentRefund");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRefund_RentalPaymentId",
                table: "PaymentRefund");

            migrationBuilder.DropIndex(
                name: "UX_PaymentRefund_StripeRefundId",
                table: "PaymentRefund");

            migrationBuilder.AlterColumn<int>(
                name: "RentalPaymentId",
                table: "PaymentRefund",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalRefund",
                table: "PaymentRefund",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRefund_RentalPayment_RentalPaymentId",
                table: "PaymentRefund",
                column: "RentalPaymentId",
                principalTable: "RentalPayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_RentalRefund_RentalPaymentId",
                table: "PaymentRefund",
                column: "RentalPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalRefund_StripeRefundId",
                table: "PaymentRefund",
                column: "StripeRefundId",
                unique: true);

            migrationBuilder.RenameTable(
                name: "PaymentRefund",
                newName: "RentalRefund");
        }
    }
}

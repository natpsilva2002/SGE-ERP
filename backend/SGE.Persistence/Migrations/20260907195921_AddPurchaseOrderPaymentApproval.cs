using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderPaymentApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentApprovedAt",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentApprovedByUserId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PaymentApprovedByUserId",
                table: "PurchaseOrders",
                column: "PaymentApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_users_PaymentApprovedByUserId",
                table: "PurchaseOrders",
                column: "PaymentApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_users_PaymentApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PaymentApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PaymentApprovedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PaymentApprovedByUserId",
                table: "PurchaseOrders");
        }
    }
}

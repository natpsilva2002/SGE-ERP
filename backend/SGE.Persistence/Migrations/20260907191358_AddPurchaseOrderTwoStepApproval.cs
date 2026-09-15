using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderTwoStepApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstApprovedAt",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FirstApprovedByUserId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecondApprovedAt",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondApprovedByUserId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_FirstApprovedByUserId",
                table: "PurchaseOrders",
                column: "FirstApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SecondApprovedByUserId",
                table: "PurchaseOrders",
                column: "SecondApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_users_FirstApprovedByUserId",
                table: "PurchaseOrders",
                column: "FirstApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_users_SecondApprovedByUserId",
                table: "PurchaseOrders",
                column: "SecondApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_users_FirstApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_users_SecondApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_FirstApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SecondApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FirstApprovedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FirstApprovedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SecondApprovedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SecondApprovedByUserId",
                table: "PurchaseOrders");
        }
    }
}

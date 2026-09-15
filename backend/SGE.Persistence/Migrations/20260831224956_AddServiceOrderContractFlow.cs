using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderContractFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_PurchaseRequestId",
                table: "ServiceOrders");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "ServiceOrders",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractUploadedAt",
                table: "ServiceOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContractUploadedByUserId",
                table: "ServiceOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_ContractUploadedByUserId",
                table: "ServiceOrders",
                column: "ContractUploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_Number",
                table: "ServiceOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_PurchaseRequestId",
                table: "ServiceOrders",
                column: "PurchaseRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_users_ContractUploadedByUserId",
                table: "ServiceOrders",
                column: "ContractUploadedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_users_ContractUploadedByUserId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_ContractUploadedByUserId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_Number",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_PurchaseRequestId",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "ContractUploadedAt",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "ContractUploadedByUserId",
                table: "ServiceOrders");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "ServiceOrders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_PurchaseRequestId",
                table: "ServiceOrders",
                column: "PurchaseRequestId");
        }
    }
}

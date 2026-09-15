using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchasingApprovalAndOrderFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_purchase_requests_PurchaseRequestId",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ApprovalHistories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PurchaseOrders");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "PurchaseOrderItems",
                newName: "QuantityOrdered");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "quotations",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "PurchaseOrders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "PurchaseOrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseRequestId",
                table: "Approvals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovalDate",
                table: "Approvals",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "QuotationId",
                table: "Approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Approvals",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Approvals",
                type: "integer",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ApprovalHistories",
                type: "integer",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_QuotationId",
                table: "Approvals",
                column: "QuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_purchase_requests_PurchaseRequestId",
                table: "Approvals",
                column: "PurchaseRequestId",
                principalTable: "purchase_requests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_quotations_QuotationId",
                table: "Approvals",
                column: "QuotationId",
                principalTable: "quotations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_purchase_requests_PurchaseRequestId",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_quotations_QuotationId",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_QuotationId",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ApprovalHistories");

            migrationBuilder.RenameColumn(
                name: "QuantityOrdered",
                table: "PurchaseOrderItems",
                newName: "Quantity");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseOrders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "PurchaseOrders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseRequestId",
                table: "Approvals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovalDate",
                table: "Approvals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Approvals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ApprovalHistories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_purchase_requests_PurchaseRequestId",
                table: "Approvals",
                column: "PurchaseRequestId",
                principalTable: "purchase_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

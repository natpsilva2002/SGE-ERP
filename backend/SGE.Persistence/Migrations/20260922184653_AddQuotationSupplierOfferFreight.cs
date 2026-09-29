using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationSupplierOfferFreight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FreightValue",
                table: "PurchaseOrders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "QuotationSupplierOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    FreightValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationSupplierOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationSupplierOffers_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuotationSupplierOffers_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationSupplierOffers_QuotationId_SupplierId",
                table: "QuotationSupplierOffers",
                columns: new[] { "QuotationId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationSupplierOffers_SupplierId",
                table: "QuotationSupplierOffers",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotationSupplierOffers");

            migrationBuilder.DropColumn(
                name: "FreightValue",
                table: "PurchaseOrders");
        }
    }
}

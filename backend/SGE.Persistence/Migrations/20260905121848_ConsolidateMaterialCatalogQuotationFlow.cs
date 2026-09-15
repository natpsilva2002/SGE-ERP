using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateMaterialCatalogQuotationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Complement",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "StateRegistration",
                table: "suppliers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentCondition",
                table: "quotation_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposalNumber",
                table: "quotation_items",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Complement",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "StateRegistration",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentCondition",
                table: "quotation_items");

            migrationBuilder.DropColumn(
                name: "ProposalNumber",
                table: "quotation_items");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "items");
        }
    }
}

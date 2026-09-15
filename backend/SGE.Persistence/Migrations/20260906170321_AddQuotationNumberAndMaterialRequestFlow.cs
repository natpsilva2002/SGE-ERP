using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationNumberAndMaterialRequestFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "quotations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE quotations
                SET "Number" = 'CT-' || to_char("QuotationDate", 'YYYYMMDD') || '-' || upper(substr(replace("Id"::text, '-', ''), 1, 8))
                WHERE "Number" IS NULL OR "Number" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "quotations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotations_Number",
                table: "quotations",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quotations_Number",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "quotations");
        }
    }
}

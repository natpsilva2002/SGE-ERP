using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasuresAndReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceUnitOfMeasureId",
                table: "purchase_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasureId",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "unit_of_measures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_of_measures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requests_ServiceUnitOfMeasureId",
                table: "purchase_requests",
                column: "ServiceUnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_items_UnitOfMeasureId",
                table: "items",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_Code",
                table: "unit_of_measures",
                column: "Code",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO unit_of_measures ("Id", "Code", "Description", "CreatedAt", "IsDeleted")
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 'UN', 'Unidade', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000002', 'PC', 'Peça', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000003', 'CX', 'Caixa', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000004', 'SC', 'Saco', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000005', 'KG', 'Quilograma', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000006', 'G', 'Grama', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000007', 'T', 'Tonelada', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000008', 'M', 'Metro', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000009', 'M²', 'Metro quadrado', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000010', 'M³', 'Metro cúbico', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000011', 'L', 'Litro', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000012', 'ML', 'Mililitro', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000013', 'RL', 'Rolo', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000014', 'BD', 'Balde', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000015', 'LT', 'Lote', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000016', 'H', 'Hora', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000017', 'KM', 'Quilômetro', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000018', 'TON', 'Tonelada (t)', CURRENT_TIMESTAMP, FALSE),
                    ('10000000-0000-0000-0000-000000000019', 'DIÁRIA', 'Diária', CURRENT_TIMESTAMP, FALSE);

                UPDATE items AS i
                SET "UnitOfMeasureId" = u."Id"
                FROM unit_of_measures AS u
                WHERE i."UnitOfMeasureId" IS NULL
                  AND upper(trim(i."Unit")) = u."Code";

                UPDATE purchase_requests AS p
                SET "ServiceUnitOfMeasureId" = u."Id"
                FROM unit_of_measures AS u
                WHERE p."ServiceUnitOfMeasureId" IS NULL
                  AND upper(trim(p."ServiceUnit")) = u."Code";
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_items_unit_of_measures_UnitOfMeasureId",
                table: "items",
                column: "UnitOfMeasureId",
                principalTable: "unit_of_measures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_requests_unit_of_measures_ServiceUnitOfMeasureId",
                table: "purchase_requests",
                column: "ServiceUnitOfMeasureId",
                principalTable: "unit_of_measures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_unit_of_measures_UnitOfMeasureId",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_requests_unit_of_measures_ServiceUnitOfMeasureId",
                table: "purchase_requests");

            migrationBuilder.DropTable(
                name: "unit_of_measures");

            migrationBuilder.DropIndex(
                name: "IX_purchase_requests_ServiceUnitOfMeasureId",
                table: "purchase_requests");

            migrationBuilder.DropIndex(
                name: "IX_items_UnitOfMeasureId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ServiceUnitOfMeasureId",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureId",
                table: "items");
        }
    }
}

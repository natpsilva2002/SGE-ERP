using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakePurchaseRequestCompanyRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM companies
                        WHERE "TradeName" = 'Estrutural'
                           OR "CorporateName" = 'Estrutural') THEN
                        RAISE EXCEPTION 'A empresa Estrutural nao esta cadastrada. Cadastre-a antes de aplicar esta migration.';
                    END IF;
                END $$;

                UPDATE purchase_requests
                SET "CompanyId" = companies."Id"
                FROM companies
                WHERE purchase_requests."CompanyId" IS NULL
                  AND (companies."TradeName" = 'Estrutural'
                       OR companies."CorporateName" = 'Estrutural');
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "purchase_requests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "purchase_requests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}

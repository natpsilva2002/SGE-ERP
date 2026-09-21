using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations;

public partial class SeedStructuralCompany : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO companies
                ("Id", "CorporateName", "TradeName", "Document", "Email", "Phone",
                 "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy")
            SELECT
                '11111111-1111-1111-1111-111111111111',
                'Estrutural',
                'Estrutural',
                'ESTRUTURAL-SGE',
                '',
                '',
                NOW(),
                NULL,
                NULL,
                NULL,
                FALSE,
                NULL,
                NULL
            WHERE NOT EXISTS (
                SELECT 1
                FROM companies
                WHERE "TradeName" = 'Estrutural'
                   OR "CorporateName" = 'Estrutural');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep the base company and any references created after this migration.
    }
}

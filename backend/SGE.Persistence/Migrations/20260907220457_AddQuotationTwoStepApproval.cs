using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationTwoStepApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstApprovedAt",
                table: "quotations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FirstApprovedByUserId",
                table: "quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecondApprovedAt",
                table: "quotations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondApprovedByUserId",
                table: "quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotations_FirstApprovedByUserId",
                table: "quotations",
                column: "FirstApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_SecondApprovedByUserId",
                table: "quotations",
                column: "SecondApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_quotations_users_FirstApprovedByUserId",
                table: "quotations",
                column: "FirstApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quotations_users_SecondApprovedByUserId",
                table: "quotations",
                column: "SecondApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quotations_users_FirstApprovedByUserId",
                table: "quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_quotations_users_SecondApprovedByUserId",
                table: "quotations");

            migrationBuilder.DropIndex(
                name: "IX_quotations_FirstApprovedByUserId",
                table: "quotations");

            migrationBuilder.DropIndex(
                name: "IX_quotations_SecondApprovedByUserId",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "FirstApprovedAt",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "FirstApprovedByUserId",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "SecondApprovedAt",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "SecondApprovedByUserId",
                table: "quotations");
        }
    }
}

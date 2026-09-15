using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteServiceMeasurementFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceMeasurements_ServiceOrderId",
                table: "ServiceMeasurements");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ServiceMeasurements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "ServiceMeasurements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementNumber",
                table: "ServiceMeasurements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "ServiceMeasurements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByUserId",
                table: "ServiceMeasurements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ServiceMeasurements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ServiceMeasurements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeasurements_ApprovedByUserId",
                table: "ServiceMeasurements",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeasurements_RejectedByUserId",
                table: "ServiceMeasurements",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeasurements_ServiceOrderId_MeasurementNumber",
                table: "ServiceMeasurements",
                columns: new[] { "ServiceOrderId", "MeasurementNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceMeasurements_users_ApprovedByUserId",
                table: "ServiceMeasurements",
                column: "ApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceMeasurements_users_RejectedByUserId",
                table: "ServiceMeasurements",
                column: "RejectedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceMeasurements_users_ApprovedByUserId",
                table: "ServiceMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceMeasurements_users_RejectedByUserId",
                table: "ServiceMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ServiceMeasurements_ApprovedByUserId",
                table: "ServiceMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ServiceMeasurements_RejectedByUserId",
                table: "ServiceMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ServiceMeasurements_ServiceOrderId_MeasurementNumber",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "MeasurementNumber",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ServiceMeasurements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ServiceMeasurements");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeasurements_ServiceOrderId",
                table: "ServiceMeasurements",
                column: "ServiceOrderId");
        }
    }
}

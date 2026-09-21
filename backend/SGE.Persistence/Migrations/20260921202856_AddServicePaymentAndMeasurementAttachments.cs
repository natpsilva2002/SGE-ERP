using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePaymentAndMeasurementAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceMeasurementAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceMeasurementId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMeasurementAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMeasurementAttachments_ServiceMeasurements_ServiceMe~",
                        column: x => x.ServiceMeasurementId,
                        principalTable: "ServiceMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceMeasurementAttachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrderPaymentAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderPaymentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderPaymentAttachments_ServiceOrderPayments_Service~",
                        column: x => x.ServiceOrderPaymentId,
                        principalTable: "ServiceOrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOrderPaymentAttachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeasurementAttachments_ServiceMeasurementId",
                table: "ServiceMeasurementAttachments",
                column: "ServiceMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeasurementAttachments_UploadedByUserId",
                table: "ServiceMeasurementAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderPaymentAttachments_ServiceOrderPaymentId",
                table: "ServiceOrderPaymentAttachments",
                column: "ServiceOrderPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderPaymentAttachments_UploadedByUserId",
                table: "ServiceOrderPaymentAttachments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceMeasurementAttachments");

            migrationBuilder.DropTable(
                name: "ServiceOrderPaymentAttachments");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderAdvancePaymentsAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdvancePaymentRequestId",
                table: "ServiceOrderPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceAdvancePaymentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAdvancePaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceAdvancePaymentRequests_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceAdvancePaymentRequests_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceAdvancePaymentRequests_users_RejectedByUserId",
                        column: x => x.RejectedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceAdvancePaymentRequests_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrderAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAttachments_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAttachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderPayments_AdvancePaymentRequestId",
                table: "ServiceOrderPayments",
                column: "AdvancePaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAdvancePaymentRequests_ApprovedByUserId",
                table: "ServiceAdvancePaymentRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAdvancePaymentRequests_RejectedByUserId",
                table: "ServiceAdvancePaymentRequests",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAdvancePaymentRequests_RequestedByUserId",
                table: "ServiceAdvancePaymentRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAdvancePaymentRequests_ServiceOrderId",
                table: "ServiceAdvancePaymentRequests",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAttachments_ServiceOrderId",
                table: "ServiceOrderAttachments",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAttachments_UploadedByUserId",
                table: "ServiceOrderAttachments",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderPayments_ServiceAdvancePaymentRequests_AdvanceP~",
                table: "ServiceOrderPayments",
                column: "AdvancePaymentRequestId",
                principalTable: "ServiceAdvancePaymentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderPayments_ServiceAdvancePaymentRequests_AdvanceP~",
                table: "ServiceOrderPayments");

            migrationBuilder.DropTable(
                name: "ServiceAdvancePaymentRequests");

            migrationBuilder.DropTable(
                name: "ServiceOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrderPayments_AdvancePaymentRequestId",
                table: "ServiceOrderPayments");

            migrationBuilder.DropColumn(
                name: "AdvancePaymentRequestId",
                table: "ServiceOrderPayments");
        }
    }
}

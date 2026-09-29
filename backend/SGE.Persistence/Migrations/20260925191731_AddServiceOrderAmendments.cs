using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOrderAmendmentId",
                table: "Approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceOrderAmendments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ValueAdjustment = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    QuantityAdjustment = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Observation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValueBeforeApproval = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ValueAfterApproval = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    QuantityBeforeApproval = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    QuantityAfterApproval = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAmendments_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAmendments_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAmendments_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrderAmendmentAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOrderAmendmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderAmendmentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAmendmentAttachments_ServiceOrderAmendments_Ser~",
                        column: x => x.ServiceOrderAmendmentId,
                        principalTable: "ServiceOrderAmendments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceOrderAmendmentAttachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ServiceOrderAmendmentId",
                table: "Approvals",
                column: "ServiceOrderAmendmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAmendmentAttachments_ServiceOrderAmendmentId",
                table: "ServiceOrderAmendmentAttachments",
                column: "ServiceOrderAmendmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAmendmentAttachments_UploadedByUserId",
                table: "ServiceOrderAmendmentAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAmendments_ApprovedByUserId",
                table: "ServiceOrderAmendments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAmendments_CreatedByUserId",
                table: "ServiceOrderAmendments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderAmendments_ServiceOrderId",
                table: "ServiceOrderAmendments",
                column: "ServiceOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_ServiceOrderAmendments_ServiceOrderAmendmentId",
                table: "Approvals",
                column: "ServiceOrderAmendmentId",
                principalTable: "ServiceOrderAmendments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_ServiceOrderAmendments_ServiceOrderAmendmentId",
                table: "Approvals");

            migrationBuilder.DropTable(
                name: "ServiceOrderAmendmentAttachments");

            migrationBuilder.DropTable(
                name: "ServiceOrderAmendments");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_ServiceOrderAmendmentId",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "ServiceOrderAmendmentId",
                table: "Approvals");
        }
    }
}

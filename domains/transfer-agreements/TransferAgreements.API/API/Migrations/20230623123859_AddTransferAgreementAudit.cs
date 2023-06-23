using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAgreementAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransferAgreementAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferAgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    ActorName = table.Column<string>(type: "text", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderName = table.Column<string>(type: "text", nullable: false),
                    SenderTin = table.Column<string>(type: "text", nullable: false),
                    ReceiverTin = table.Column<string>(type: "text", nullable: false),
                    AuditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuditAction = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferAgreementAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferAgreementAudits_TransferAgreements_TransferAgreemen~",
                        column: x => x.TransferAgreementId,
                        principalTable: "TransferAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferAgreementAudits_TransferAgreementId",
                table: "TransferAgreementAudits",
                column: "TransferAgreementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferAgreementAudits");
        }
    }
}

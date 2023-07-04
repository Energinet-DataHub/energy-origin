using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAgreementHistoryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "TransferAgreements");

            migrationBuilder.CreateTable(
                name: "TransferAgreementHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferAgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    ActorName = table.Column<string>(type: "text", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderName = table.Column<string>(type: "text", nullable: false),
                    SenderTin = table.Column<string>(type: "text", nullable: false),
                    ReceiverTin = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuditAction = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferAgreementHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferAgreementHistoryEntries_TransferAgreements_Transfer~",
                        column: x => x.TransferAgreementId,
                        principalTable: "TransferAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferAgreementHistoryEntries_TransferAgreementId",
                table: "TransferAgreementHistoryEntries",
                column: "TransferAgreementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferAgreementHistoryEntries");

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "TransferAgreements",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

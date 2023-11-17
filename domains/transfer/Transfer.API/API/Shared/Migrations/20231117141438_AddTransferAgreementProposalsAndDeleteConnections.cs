using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAgreementProposalsAndDeleteConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionInvitations");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.CreateTable(
                name: "TransferAgreementProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyTin = table.Column<string>(type: "text", nullable: false),
                    SenderCompanyName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'UTC'"),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceiverCompanyTin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferAgreementProposals", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferAgreementProposals");

            migrationBuilder.CreateTable(
                name: "ConnectionInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'UTC'"),
                    SenderCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyTin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionInvitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyAId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyATin = table.Column<string>(type: "text", nullable: false),
                    CompanyBId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyBTin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CompanyAId",
                table: "Connections",
                column: "CompanyAId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CompanyBId",
                table: "Connections",
                column: "CompanyBId");
        }
    }
}

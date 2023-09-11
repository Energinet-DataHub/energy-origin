using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<int>(type: "integer", nullable: false),
                    GSRN = table.Column<string>(type: "text", nullable: false),
                    GridArea = table.Column<string>(type: "text", nullable: false),
                    MeteringPointType = table.Column<int>(type: "integer", nullable: false),
                    MeteringPointOwner = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WalletUrl = table.Column<string>(type: "text", nullable: false),
                    WalletPublicKey = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedState = table.Column<int>(type: "integer", nullable: false),
                    GridArea = table.Column<string>(type: "text", nullable: false),
                    DateFrom = table.Column<long>(type: "bigint", nullable: false),
                    DateTo = table.Column<long>(type: "bigint", nullable: false),
                    Technology_FuelCode = table.Column<string>(type: "text", nullable: false),
                    Technology_TechCode = table.Column<string>(type: "text", nullable: false),
                    MeteringPointOwner = table.Column<string>(type: "text", nullable: false),
                    Gsrn = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    BlindingValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SynchronizationPositions",
                columns: table => new
                {
                    GSRN = table.Column<string>(type: "text", nullable: false),
                    SyncedTo = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronizationPositions", x => x.GSRN);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_GSRN_ContractNumber",
                table: "Contracts",
                columns: new[] { "GSRN", "ContractNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCertificates_Gsrn_DateFrom_DateTo",
                table: "ProductionCertificates",
                columns: new[] { "Gsrn", "DateFrom", "DateTo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "ProductionCertificates");

            migrationBuilder.DropTable(
                name: "SynchronizationPositions");
        }
    }
}

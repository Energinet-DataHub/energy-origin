using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedContractsToUseRecipientIdAndRemovedCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM public.\"Contracts\"");

            migrationBuilder.DropTable(
                name: "ConsumptionCertificates");

            migrationBuilder.DropTable(
                name: "ProductionCertificates");

            migrationBuilder.DropColumn(
                name: "WalletPublicKey",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "WalletUrl",
                table: "Contracts");

            migrationBuilder.AddColumn<Guid>(
                name: "RecipientId",
                table: "Contracts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientId",
                table: "Contracts");

            migrationBuilder.AddColumn<byte[]>(
                name: "WalletPublicKey",
                table: "Contracts",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "WalletUrl",
                table: "Contracts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ConsumptionCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlindingValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    DateFrom = table.Column<long>(type: "bigint", nullable: false),
                    DateTo = table.Column<long>(type: "bigint", nullable: false),
                    GridArea = table.Column<string>(type: "text", nullable: false),
                    Gsrn = table.Column<string>(type: "text", nullable: false),
                    IssuedState = table.Column<int>(type: "integer", nullable: false),
                    MeteringPointOwner = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlindingValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    DateFrom = table.Column<long>(type: "bigint", nullable: false),
                    DateTo = table.Column<long>(type: "bigint", nullable: false),
                    GridArea = table.Column<string>(type: "text", nullable: false),
                    Gsrn = table.Column<string>(type: "text", nullable: false),
                    IssuedState = table.Column<int>(type: "integer", nullable: false),
                    MeteringPointOwner = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    Technology_FuelCode = table.Column<string>(type: "text", nullable: false),
                    Technology_TechCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCertificates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionCertificates_Gsrn_DateFrom_DateTo",
                table: "ConsumptionCertificates",
                columns: new[] { "Gsrn", "DateFrom", "DateTo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCertificates_Gsrn_DateFrom_DateTo",
                table: "ProductionCertificates",
                columns: new[] { "Gsrn", "DateFrom", "DateTo" },
                unique: true);
        }
    }
}

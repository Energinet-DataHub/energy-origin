using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddedConsumptionCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumptionCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedState = table.Column<int>(type: "integer", nullable: false),
                    GridArea = table.Column<string>(type: "text", nullable: false),
                    DateFrom = table.Column<long>(type: "bigint", nullable: false),
                    DateTo = table.Column<long>(type: "bigint", nullable: false),
                    MeteringPointOwner = table.Column<string>(type: "text", nullable: false),
                    Gsrn = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    BlindingValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionCertificates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionCertificates_Gsrn_DateFrom_DateTo",
                table: "ConsumptionCertificates",
                columns: new[] { "Gsrn", "DateFrom", "DateTo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumptionCertificates");
        }
    }
}

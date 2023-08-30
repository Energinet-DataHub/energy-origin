using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Cert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedState = table.Column<int>(type: "integer", nullable: false),
                    GridArea = table.Column<string>(type: "text", nullable: false),
                    Period_DateFrom = table.Column<long>(type: "bigint", nullable: false),
                    Period_DateTo = table.Column<long>(type: "bigint", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionCertificates");
        }
    }
}

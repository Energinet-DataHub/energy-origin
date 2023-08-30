using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Unique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Period_DateTo",
                table: "ProductionCertificates",
                newName: "DateTo");

            migrationBuilder.RenameColumn(
                name: "Period_DateFrom",
                table: "ProductionCertificates",
                newName: "DateFrom");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCertificates_Gsrn_DateFrom_DateTo",
                table: "ProductionCertificates",
                columns: new[] { "Gsrn", "DateFrom", "DateTo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionCertificates_Gsrn_DateFrom_DateTo",
                table: "ProductionCertificates");

            migrationBuilder.RenameColumn(
                name: "DateTo",
                table: "ProductionCertificates",
                newName: "Period_DateTo");

            migrationBuilder.RenameColumn(
                name: "DateFrom",
                table: "ProductionCertificates",
                newName: "Period_DateFrom");
        }
    }
}

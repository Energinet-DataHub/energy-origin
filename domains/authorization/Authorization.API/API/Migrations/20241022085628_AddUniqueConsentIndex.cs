using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConsentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationConsents_ConsentReceiverOrganizationId",
                table: "OrganizationConsents");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationConsents_ConsentReceiverOrganizationId_ConsentG~",
                table: "OrganizationConsents",
                columns: new[] { "ConsentReceiverOrganizationId", "ConsentGiverOrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationConsents_ConsentReceiverOrganizationId_ConsentG~",
                table: "OrganizationConsents");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationConsents_ConsentReceiverOrganizationId",
                table: "OrganizationConsents",
                column: "ConsentReceiverOrganizationId");
        }
    }
}

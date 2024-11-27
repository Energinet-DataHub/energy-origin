using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class ChangeServiceProviderTermsToAcceptedForClientOrganizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Organizations""
                SET ""ServiceProviderTermsAccepted"" = TRUE,
                    ""ServiceProviderTermsAcceptanceDate"" = NOW()
                WHERE ""Id"" IN (
                    SELECT DISTINCT ""OrganizationId""
                    FROM ""Clients""
                    WHERE ""OrganizationId"" IS NOT NULL
                )
                AND (""ServiceProviderTermsAccepted"" = FALSE OR ""ServiceProviderTermsAccepted"" IS NULL);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

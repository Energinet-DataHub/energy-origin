using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultServiceProviderTermsWithUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ServiceProviderTerms",
                columns: new[] { "Id", "Version" },
                values: new object[] { new Guid("a545358f-0475-43b4-a911-6fa8009ec0da"), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderTerms_Version",
                table: "ServiceProviderTerms",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceProviderTerms_Version",
                table: "ServiceProviderTerms");

            migrationBuilder.DeleteData(
                table: "ServiceProviderTerms",
                keyColumn: "Id",
                keyValue: new Guid("a545358f-0475-43b4-a911-6fa8009ec0da"));
        }
    }
}

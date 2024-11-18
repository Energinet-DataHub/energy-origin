using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProviderTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ServiceProviderTermsAcceptanceDate",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ServiceProviderTermsAccepted",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ServiceProviderTermsVersion",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceProviderTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviderTerms", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceProviderTerms");

            migrationBuilder.DropColumn(
                name: "ServiceProviderTermsAcceptanceDate",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ServiceProviderTermsAccepted",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ServiceProviderTermsVersion",
                table: "Organizations");
        }
    }
}

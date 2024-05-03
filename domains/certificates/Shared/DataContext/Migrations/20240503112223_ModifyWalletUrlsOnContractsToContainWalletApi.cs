using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class ModifyWalletUrlsOnContractsToContainWalletApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE public.\"Contracts\" SET \"WalletUrl\" = REPLACE(\"WalletUrl\", '/v1/slices', '/wallet-api/v1/slices');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE public.\"Contracts\" SET \"WalletUrl\" = REPLACE(\"WalletUrl\", '/wallet-api/v1/slices', '/v1/slices');");
        }
    }
}

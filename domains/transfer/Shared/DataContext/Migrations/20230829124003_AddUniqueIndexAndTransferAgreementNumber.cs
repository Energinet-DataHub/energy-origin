#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexAndTransferAgreementNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransferAgreementNumber",
                table: "TransferAgreements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TransferAgreements_SenderId_TransferAgreementNumber",
                table: "TransferAgreements",
                columns: new[] { "SenderId", "TransferAgreementNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransferAgreements_SenderId_TransferAgreementNumber",
                table: "TransferAgreements");

            migrationBuilder.DropColumn(
                name: "TransferAgreementNumber",
                table: "TransferAgreements");
        }
    }
}

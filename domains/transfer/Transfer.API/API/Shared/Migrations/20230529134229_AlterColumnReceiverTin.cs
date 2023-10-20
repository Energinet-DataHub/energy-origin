#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AlterColumnReceiverTin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReceiverTin",
                table: "TransferAgreements",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var agreementsTable = "\"TransferAgreements\"";
            var receiverTinCol = "\"ReceiverTin\"";

            migrationBuilder.Sql($"ALTER TABLE {agreementsTable} ALTER COLUMN {receiverTinCol} TYPE integer USING ({receiverTinCol}::integer);");
        }
    }
}

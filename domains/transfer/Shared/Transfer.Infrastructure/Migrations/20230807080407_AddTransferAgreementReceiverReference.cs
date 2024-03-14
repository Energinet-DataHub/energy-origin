#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferAgreementReceiverReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverReference",
                table: "TransferAgreements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ReceiverReference",
                table: "TransferAgreements",
                type: "uuid",
                nullable: false,
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverReference",
                table: "TransferAgreements");
        }
    }
}

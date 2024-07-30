using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTermsWithUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Terms",
                columns: new[] { "Id", "Version" },
                values: new object[] { new Guid("0ccb0348-3179-4b96-9be0-dc7ab1541771"), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Version",
                table: "Terms",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Terms_Version",
                table: "Terms");

            migrationBuilder.DeleteData(
                table: "Terms",
                keyColumn: "Id",
                keyValue: new Guid("0ccb0348-3179-4b96-9be0-dc7ab1541771"));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToTermsVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Terms",
                keyColumn: "Id",
                keyValue: new Guid("9d07429a-d98e-443a-834f-7d87b10e7f3a"));

            migrationBuilder.InsertData(
                table: "Terms",
                columns: new[] { "Id", "Version" },
                values: new object[] { new Guid("a2b7a580-fb8c-4ec2-b254-33d07a677369"), 1 });

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
                keyValue: new Guid("a2b7a580-fb8c-4ec2-b254-33d07a677369"));

            migrationBuilder.InsertData(
                table: "Terms",
                columns: new[] { "Id", "Version" },
                values: new object[] { new Guid("9d07429a-d98e-443a-834f-7d87b10e7f3a"), 1 });
        }
    }
}

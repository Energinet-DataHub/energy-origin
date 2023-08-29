using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Connection",
                schema: "con",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyAId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyATin = table.Column<string>(type: "text", nullable: false),
                    CompanyBId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyBTin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connection", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connection_CompanyAId",
                schema: "con",
                table: "Connection",
                column: "CompanyAId");

            migrationBuilder.CreateIndex(
                name: "IX_Connection_CompanyBId",
                schema: "con",
                table: "Connection",
                column: "CompanyBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connection",
                schema: "con");
        }
    }
}

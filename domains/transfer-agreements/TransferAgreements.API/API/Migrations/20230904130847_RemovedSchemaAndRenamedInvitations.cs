using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class RemovedSchemaAndRenamedInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitation",
                schema: "con");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Connection",
                schema: "con",
                table: "Connection");

            migrationBuilder.RenameTable(
                name: "Connection",
                schema: "con",
                newName: "Connections");

            migrationBuilder.RenameIndex(
                name: "IX_Connection_CompanyBId",
                table: "Connections",
                newName: "IX_Connections_CompanyBId");

            migrationBuilder.RenameIndex(
                name: "IX_Connection_CompanyAId",
                table: "Connections",
                newName: "IX_Connections_CompanyAId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Connections",
                table: "Connections",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ConnectionInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyTin = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionInvitations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionInvitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Connections",
                table: "Connections");

            migrationBuilder.EnsureSchema(
                name: "con");

            migrationBuilder.RenameTable(
                name: "Connections",
                newName: "Connection",
                newSchema: "con");

            migrationBuilder.RenameIndex(
                name: "IX_Connections_CompanyBId",
                schema: "con",
                table: "Connection",
                newName: "IX_Connection_CompanyBId");

            migrationBuilder.RenameIndex(
                name: "IX_Connections_CompanyAId",
                schema: "con",
                table: "Connection",
                newName: "IX_Connection_CompanyAId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Connection",
                schema: "con",
                table: "Connection",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Invitation",
                schema: "con",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'UTC'"),
                    SenderCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyTin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitation", x => x.Id);
                });
        }
    }
}

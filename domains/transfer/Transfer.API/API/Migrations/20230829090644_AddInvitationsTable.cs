using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "con");

            migrationBuilder.CreateTable(
                name: "Invitation",
                schema: "con",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderCompanyTin = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitation", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitation",
                schema: "con");
        }
    }
}

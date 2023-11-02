using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimAutomationArgument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimAutomationArguments",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAutomationArguments", x => x.SubjectId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimAutomationArguments");
        }
    }
}

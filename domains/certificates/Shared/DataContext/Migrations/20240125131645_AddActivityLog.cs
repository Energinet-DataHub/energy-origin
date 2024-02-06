using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorType = table.Column<int>(type: "integer", nullable: false),
                    ActorName = table.Column<string>(type: "text", nullable: false),
                    OrganizationTin = table.Column<string>(type: "text", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_OrganizationTin",
                table: "ActivityLogs",
                column: "OrganizationTin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");
        }
    }
}

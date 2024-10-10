using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Tin",
                table: "Organizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentGiverOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentReceiverOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationConsents_Organizations_ConsentGiverOrganization~",
                        column: x => x.ConsentGiverOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationConsents_Organizations_ConsentReceiverOrganizat~",
                        column: x => x.ConsentReceiverOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_OrganizationId",
                table: "Clients",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationConsents_ConsentGiverOrganizationId",
                table: "OrganizationConsents",
                column: "ConsentGiverOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationConsents_ConsentReceiverOrganizationId",
                table: "OrganizationConsents",
                column: "ConsentReceiverOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Organizations_OrganizationId",
                table: "Clients",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

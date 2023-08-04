﻿using System;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.AutoGenerated.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedTermsVersion",
                table: "Users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:company_terms_type", "terms_of_service")
                .Annotation("Npgsql:Enum:provider_key_type", "pid,rid,eia,mit_id_uuid")
                .Annotation("Npgsql:Enum:user_terms_type", "privacy_policy")
                .OldAnnotation("Npgsql:Enum:provider_key_type", "pid,rid,eia,mit_id_uuid");

            migrationBuilder.CreateTable(
                name: "CompanyTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<CompanyTermsType>(type: "company_terms_type", nullable: false),
                    AcceptedVersion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyTerms_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<UserTermsType>(type: "user_terms_type", nullable: false),
                    AcceptedVersion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTerms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTerms_CompanyId",
                table: "CompanyTerms",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Key",
                table: "Roles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTerms_UserId",
                table: "UserTerms",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyTerms");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTerms");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:provider_key_type", "pid,rid,eia,mit_id_uuid")
                .OldAnnotation("Npgsql:Enum:company_terms_type", "terms_of_service")
                .OldAnnotation("Npgsql:Enum:provider_key_type", "pid,rid,eia,mit_id_uuid")
                .OldAnnotation("Npgsql:Enum:user_terms_type", "privacy_policy");

            migrationBuilder.AddColumn<int>(
                name: "AcceptedTermsVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

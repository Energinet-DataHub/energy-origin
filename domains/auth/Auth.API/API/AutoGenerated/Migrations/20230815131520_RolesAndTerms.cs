﻿using System;
using API.Values;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.AutoGenerated.Migrations
{
    /// <inheritdoc />
    public partial class RolesAndTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    AcceptedVersion = table.Column<int>(type: "integer", nullable: false)
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
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<UserTermsType>(type: "user_terms_type", nullable: false),
                    AcceptedVersion = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTerms_CompanyId_Type",
                table: "CompanyTerms",
                columns: new[] { "CompanyId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_Role",
                table: "UserRoles",
                columns: new[] { "UserId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTerms_UserId_Type",
                table: "UserTerms",
                columns: new[] { "UserId", "Type" },
                unique: true);
            
            migrationBuilder.Sql("""
                INSERT INTO "UserTerms" ("Id", "UserId", "AcceptedVersion", "Type")
                SELECT gen_random_uuid(), "Id", "AcceptedTermsVersion", 'privacy_policy' FROM "Users"
            """);

            migrationBuilder.DropColumn(
                name: "AcceptedTermsVersion",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyTerms");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.AddColumn<int>(
                name: "AcceptedTermsVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "AcceptedTermsVersion" = "UserTerms"."AcceptedVersion"
                FROM "UserTerms"
                WHERE "Users"."Id" = "UserTerms"."UserId"
            """);
            
            migrationBuilder.DropTable(
                name: "UserTerms");
            
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:provider_key_type", "pid,rid,eia,mit_id_uuid")
                .OldAnnotation("Npgsql:Enum:company_terms_type", "terms_of_service")
                .OldAnnotation("Npgsql:Enum:provider_key_type", "pid,rid,eia,mit_id_uuid")
                .OldAnnotation("Npgsql:Enum:user_terms_type", "privacy_policy");
        }
    }
}

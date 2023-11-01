using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClaimSubjectWithCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ClaimSubjectHistory",
                table: "ClaimSubjectHistory");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ClaimSubjects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClaimSubjectHistory",
                table: "ClaimSubjectHistory",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ClaimSubjectHistory",
                table: "ClaimSubjectHistory");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ClaimSubjects");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClaimSubjectHistory",
                table: "ClaimSubjectHistory",
                column: "Id");
        }
    }
}

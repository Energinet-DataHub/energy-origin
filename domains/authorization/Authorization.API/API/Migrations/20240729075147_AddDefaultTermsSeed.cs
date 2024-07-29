using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTermsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var termsExists = migrationBuilder.Sql("SELECT COUNT(1) FROM Terms WHERE Version = 1").ToString() != "0";
            if (!termsExists)
            {
                migrationBuilder.InsertData(
                    table: "Terms",
                    columns: new[] { "Id", "Version" },
                    values: new object[] { new Guid("9d07429a-d98e-443a-834f-7d87b10e7f3a"), 1 });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Terms",
                keyColumn: "Id",
                keyValue: new Guid("9d07429a-d98e-443a-834f-7d87b10e7f3a"));
        }
    }
}

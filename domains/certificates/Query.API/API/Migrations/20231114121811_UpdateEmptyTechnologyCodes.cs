using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Migrations
{
    public partial class UpdateEmptyTechnologyCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
    UPDATE ""Contracts""
    SET
        ""Technology_FuelCode"" = CASE
            WHEN ""Technology_FuelCode"" = '' THEN 'F00000000'
            ELSE ""Technology_FuelCode""
        END,
        ""Technology_TechCode"" = CASE
            WHEN ""Technology_TechCode"" = '' THEN 'T070000'
            ELSE ""Technology_TechCode""
        END
    WHERE ""Technology_FuelCode"" = '' OR ""Technology_TechCode"" = '';";

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

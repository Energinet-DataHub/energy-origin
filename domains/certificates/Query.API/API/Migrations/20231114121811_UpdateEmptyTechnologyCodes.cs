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
        ""Technology_FuelCode"" = 'F00000000',
        ""Technology_TechCode"" = 'T070000'
    WHERE ""Technology_FuelCode"" = '' AND ""Technology_TechCode"" = '' AND ""MeteringPointType"" = 0;";

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

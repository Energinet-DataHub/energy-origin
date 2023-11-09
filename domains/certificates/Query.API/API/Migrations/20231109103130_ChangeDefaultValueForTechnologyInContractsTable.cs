using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDefaultValueForTechnologyInContractsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Technology_TechCode",
                table: "Contracts",
                type: "text",
                nullable: false,
                defaultValue: "T070000",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Technology_FuelCode",
                table: "Contracts",
                type: "text",
                nullable: false,
                defaultValue: "F00000000",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Technology_TechCode",
                table: "Contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "T070000");

            migrationBuilder.AlterColumn<string>(
                name: "Technology_FuelCode",
                table: "Contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "F00000000");
        }
    }
}

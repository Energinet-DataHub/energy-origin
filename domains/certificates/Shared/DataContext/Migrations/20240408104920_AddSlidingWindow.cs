using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddSlidingWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeteringPointTimeSeriesSlidingWindows",
                columns: table => new
                {
                    GSRN = table.Column<string>(type: "text", nullable: false),
                    SynchronizationPoint = table.Column<long>(type: "bigint", nullable: false),
                    MissingMeasurements = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeteringPointTimeSeriesSlidingWindows", x => x.GSRN);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeteringPointTimeSeriesSlidingWindows");
        }
    }
}

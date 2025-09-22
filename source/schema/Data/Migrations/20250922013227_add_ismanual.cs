using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_ismanual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManual",
                table: "StudentPopulationItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManual",
                table: "StudentPopulationItem");
        }
    }
}

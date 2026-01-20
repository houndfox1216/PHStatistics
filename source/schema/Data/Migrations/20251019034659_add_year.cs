using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_year : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "week",
                table: "Mapping",
                newName: "Week");

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Mapping",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Mapping",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearStr",
                table: "Mapping",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Mapping");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Mapping");

            migrationBuilder.DropColumn(
                name: "YearStr",
                table: "Mapping");

            migrationBuilder.RenameColumn(
                name: "Week",
                table: "Mapping",
                newName: "week");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_count : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastWeekChineseCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastWeekEnglishCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastWeekLostCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThisWeekChineseCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThisWeekClassCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThisWeekCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThisWeekEnglishCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThisWeekNewCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInquiryCount",
                table: "StudentPopulation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "Type",
                table: "StudentPopulation",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastWeekChineseCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "LastWeekEnglishCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "LastWeekLostCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ThisWeekChineseCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ThisWeekClassCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ThisWeekCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ThisWeekEnglishCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ThisWeekNewCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "TotalInquiryCount",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "StudentPopulation");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseStatisticsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "StatisticsType",
                table: "Course",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDepartmentIds",
                table: "Course",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCourseIds",
                table: "Course",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GroupByClassType",
                table: "Course",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "ApplicableClassType",
                table: "Course",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SourceSubject",
                table: "Course",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatisticsType",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "SourceDepartmentIds",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "SourceCourseIds",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "GroupByClassType",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "ApplicableClassType",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "SourceSubject",
                table: "Course");
        }
    }
}

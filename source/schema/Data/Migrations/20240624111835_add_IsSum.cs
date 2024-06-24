using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_IsSum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSum",
                table: "CourseDepartment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "Subject",
                table: "CourseDepartment",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSum",
                table: "CourseDepartment");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "CourseDepartment");
        }
    }
}

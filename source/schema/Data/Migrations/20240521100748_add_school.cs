using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_school : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "StudentPopulation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulation_SchoolId",
                table: "StudentPopulation",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulation_School_SchoolId",
                table: "StudentPopulation",
                column: "SchoolId",
                principalTable: "School",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulation_School_SchoolId",
                table: "StudentPopulation");

            migrationBuilder.DropIndex(
                name: "IX_StudentPopulation_SchoolId",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentPopulation");
        }
    }
}

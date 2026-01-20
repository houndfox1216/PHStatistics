using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class remove_items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulationItem_StudentPopulation_StudentPopulationId1",
                table: "StudentPopulationItem");

            migrationBuilder.DropIndex(
                name: "IX_StudentPopulationItem_StudentPopulationId1",
                table: "StudentPopulationItem");

            migrationBuilder.DropColumn(
                name: "StudentPopulationId1",
                table: "StudentPopulationItem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StudentPopulationId1",
                table: "StudentPopulationItem",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulationItem_StudentPopulationId1",
                table: "StudentPopulationItem",
                column: "StudentPopulationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulationItem_StudentPopulation_StudentPopulationId1",
                table: "StudentPopulationItem",
                column: "StudentPopulationId1",
                principalTable: "StudentPopulation",
                principalColumn: "Id");
        }
    }
}

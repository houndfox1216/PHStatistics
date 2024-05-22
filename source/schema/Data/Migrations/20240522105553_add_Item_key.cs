using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_Item_key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulationItem_StudentPopulation_StudentPopulationId",
                table: "StudentPopulationItem");

            migrationBuilder.AlterColumn<long>(
                name: "StudentPopulationId",
                table: "StudentPopulationItem",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulationItem_StudentPopulation_StudentPopulationId",
                table: "StudentPopulationItem",
                column: "StudentPopulationId",
                principalTable: "StudentPopulation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulationItem_StudentPopulation_StudentPopulationId",
                table: "StudentPopulationItem");

            migrationBuilder.AlterColumn<long>(
                name: "StudentPopulationId",
                table: "StudentPopulationItem",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulationItem_StudentPopulation_StudentPopulationId",
                table: "StudentPopulationItem",
                column: "StudentPopulationId",
                principalTable: "StudentPopulation",
                principalColumn: "Id");
        }
    }
}

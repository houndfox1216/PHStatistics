using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberPrimarySchoolId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimarySchoolId",
                table: "Member",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Member_PrimarySchoolId",
                table: "Member",
                column: "PrimarySchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Member_School_PrimarySchoolId",
                table: "Member",
                column: "PrimarySchoolId",
                principalTable: "School",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Member_School_PrimarySchoolId",
                table: "Member");

            migrationBuilder.DropIndex(
                name: "IX_Member_PrimarySchoolId",
                table: "Member");

            migrationBuilder.DropColumn(
                name: "PrimarySchoolId",
                table: "Member");
        }
    }
}

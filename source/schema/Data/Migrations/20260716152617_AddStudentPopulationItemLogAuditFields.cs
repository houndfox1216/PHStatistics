using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPopulationItemLogAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangeLastWeekNumber",
                table: "StudentPopulationItemLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentPopulationItemLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "StudentPopulationItemLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastWeekNumber",
                table: "StudentPopulationItemLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId",
                table: "StudentPopulationItemLog",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulationItemLog_MemberId",
                table: "StudentPopulationItemLog",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulationItemLog_Member_MemberId",
                table: "StudentPopulationItemLog",
                column: "MemberId",
                principalTable: "Member",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulationItemLog_Member_MemberId",
                table: "StudentPopulationItemLog");

            migrationBuilder.DropIndex(
                name: "IX_StudentPopulationItemLog_MemberId",
                table: "StudentPopulationItemLog");

            migrationBuilder.DropColumn(
                name: "ChangeLastWeekNumber",
                table: "StudentPopulationItemLog");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudentPopulationItemLog");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "StudentPopulationItemLog");

            migrationBuilder.DropColumn(
                name: "LastWeekNumber",
                table: "StudentPopulationItemLog");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "StudentPopulationItemLog");
        }
    }
}

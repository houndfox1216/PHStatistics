using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_itemlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmTime",
                table: "StudentPopulation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmerId",
                table: "StudentPopulation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmitterId",
                table: "StudentPopulation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmitterTime",
                table: "StudentPopulation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentPopulationItemLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataMode = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SchoolName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ChangeCount = table.Column<int>(type: "int", nullable: true),
                    StudentPopulationId = table.Column<long>(type: "bigint", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentRemark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeClassId = table.Column<int>(type: "int", nullable: true),
                    ChangeNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeRemark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeStudentRemark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPopulationItemLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPopulationItemLog_Class_ChangeClassId",
                        column: x => x.ChangeClassId,
                        principalTable: "Class",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentPopulationItemLog_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentPopulationItemLog_StudentPopulation_StudentPopulationId",
                        column: x => x.StudentPopulationId,
                        principalTable: "StudentPopulation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulation_ConfirmerId",
                table: "StudentPopulation",
                column: "ConfirmerId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulation_SubmitterId",
                table: "StudentPopulation",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulationItemLog_ChangeClassId",
                table: "StudentPopulationItemLog",
                column: "ChangeClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulationItemLog_ClassId",
                table: "StudentPopulationItemLog",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPopulationItemLog_StudentPopulationId",
                table: "StudentPopulationItemLog",
                column: "StudentPopulationId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulation_Member_SubmitterId",
                table: "StudentPopulation",
                column: "SubmitterId",
                principalTable: "Member",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPopulation_User_ConfirmerId",
                table: "StudentPopulation",
                column: "ConfirmerId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulation_Member_SubmitterId",
                table: "StudentPopulation");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPopulation_User_ConfirmerId",
                table: "StudentPopulation");

            migrationBuilder.DropTable(
                name: "StudentPopulationItemLog");

            migrationBuilder.DropIndex(
                name: "IX_StudentPopulation_ConfirmerId",
                table: "StudentPopulation");

            migrationBuilder.DropIndex(
                name: "IX_StudentPopulation_SubmitterId",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ConfirmTime",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "ConfirmerId",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "StudentPopulation");

            migrationBuilder.DropColumn(
                name: "SubmitterTime",
                table: "StudentPopulation");
        }
    }
}

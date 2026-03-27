using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_new_item : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "School",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberRole",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRole", x => new { x.MemberId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_MemberRole_Member_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Member",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Region",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataMode = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Region", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_School_RegionId",
                table: "School",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRole_RoleId",
                table: "MemberRole",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_School_Region_RegionId",
                table: "School",
                column: "RegionId",
                principalTable: "Region",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_School_Region_RegionId",
                table: "School");

            migrationBuilder.DropTable(
                name: "MemberRole");

            migrationBuilder.DropTable(
                name: "Region");

            migrationBuilder.DropIndex(
                name: "IX_School_RegionId",
                table: "School");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "School");
        }
    }
}

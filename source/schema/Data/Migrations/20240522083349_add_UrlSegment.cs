using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHStatistics.Migrations
{
    /// <inheritdoc />
    public partial class add_UrlSegment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Page",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Page", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Page_MultilingualText_ContentId",
                        column: x => x.ContentId,
                        principalTable: "MultilingualText",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UrlSegment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataMode = table.Column<short>(type: "smallint", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    HasChild = table.Column<bool>(type: "bit", nullable: false),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    LinkUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlSegment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlSegment_MultilingualText_TitleId",
                        column: x => x.TitleId,
                        principalTable: "MultilingualText",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UrlSegment_Page_PageId",
                        column: x => x.PageId,
                        principalTable: "Page",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrlSegment_UrlSegment_ParentId",
                        column: x => x.ParentId,
                        principalTable: "UrlSegment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Page_ContentId",
                table: "Page",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlSegment_PageId",
                table: "UrlSegment",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlSegment_ParentId",
                table: "UrlSegment",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlSegment_TitleId",
                table: "UrlSegment",
                column: "TitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrlSegment");

            migrationBuilder.DropTable(
                name: "Page");
        }
    }
}

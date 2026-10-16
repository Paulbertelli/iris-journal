using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iris.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteRssSourceArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_RssSources_RssSourceId",
                table: "Articles");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_RssSources_RssSourceId",
                table: "Articles",
                column: "RssSourceId",
                principalTable: "RssSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_RssSources_RssSourceId",
                table: "Articles");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_RssSources_RssSourceId",
                table: "Articles",
                column: "RssSourceId",
                principalTable: "RssSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

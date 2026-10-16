using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iris.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRssSourceDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RssSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "RssSources",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "RssSources");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "RssSources");
        }
    }
}

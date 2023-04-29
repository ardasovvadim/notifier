using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifier.BackgroundService.Host.Migrations
{
    /// <inheritdoc />
    public partial class Added_LastSeason_LastEpisode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastEpisode",
                table: "MoviesRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSeason",
                table: "MoviesRecords",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastEpisode",
                table: "MoviesRecords");

            migrationBuilder.DropColumn(
                name: "LastSeason",
                table: "MoviesRecords");
        }
    }
}

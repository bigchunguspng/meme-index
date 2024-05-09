using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemeIndexCore.Migrations
{
    /// <inheritdoc />
    public partial class Sixth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonitoredDirectoryId",
                table: "Directories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Directories_MonitoredDirectoryId",
                table: "Directories",
                column: "MonitoredDirectoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Directories_MonitoredDirectories_MonitoredDirectoryId",
                table: "Directories",
                column: "MonitoredDirectoryId",
                principalTable: "MonitoredDirectories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directories_MonitoredDirectories_MonitoredDirectoryId",
                table: "Directories");

            migrationBuilder.DropIndex(
                name: "IX_Directories_MonitoredDirectoryId",
                table: "Directories");

            migrationBuilder.DropColumn(
                name: "MonitoredDirectoryId",
                table: "Directories");
        }
    }
}

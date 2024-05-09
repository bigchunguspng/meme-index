using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemeIndexCore.Migrations
{
    /// <inheritdoc />
    public partial class Second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Directory_DirectoryId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Directory",
                table: "Directory");

            migrationBuilder.RenameTable(
                name: "Directory",
                newName: "Directories");

            migrationBuilder.AddColumn<bool>(
                name: "IsTracked",
                table: "Directories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Directories",
                table: "Directories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Directories_DirectoryId",
                table: "Files",
                column: "DirectoryId",
                principalTable: "Directories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Directories_DirectoryId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Directories",
                table: "Directories");

            migrationBuilder.DropColumn(
                name: "IsTracked",
                table: "Directories");

            migrationBuilder.RenameTable(
                name: "Directories",
                newName: "Directory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Directory",
                table: "Directory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Directory_DirectoryId",
                table: "Files",
                column: "DirectoryId",
                principalTable: "Directory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

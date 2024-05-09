using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemeIndexCore.Migrations
{
    /// <inheritdoc />
    public partial class Fifth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Texts");

            migrationBuilder.DropIndex(
                name: "IX_Files_DirectoryId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Word",
                table: "Word");

            migrationBuilder.DropColumn(
                name: "IsTracked",
                table: "Directories");

            migrationBuilder.RenameTable(
                name: "Word",
                newName: "Words");

            migrationBuilder.RenameIndex(
                name: "IX_Word_Text",
                table: "Words",
                newName: "IX_Words_Text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Words",
                table: "Words",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MonitoredDirectories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DirectoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Recursive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredDirectories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitoredDirectories_Directories_DirectoryId",
                        column: x => x.DirectoryId,
                        principalTable: "Directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    WordId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tags_Means_MeanId",
                        column: x => x.MeanId,
                        principalTable: "Means",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tags_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndexingOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MonitoredDirectoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexingOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndexingOptions_Means_MeanId",
                        column: x => x.MeanId,
                        principalTable: "Means",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndexingOptions_MonitoredDirectories_MonitoredDirectoryId",
                        column: x => x.MonitoredDirectoryId,
                        principalTable: "MonitoredDirectories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_DirectoryId_Name",
                table: "Files",
                columns: new[] { "DirectoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Directories_Path",
                table: "Directories",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndexingOptions_MeanId",
                table: "IndexingOptions",
                column: "MeanId");

            migrationBuilder.CreateIndex(
                name: "IX_IndexingOptions_MonitoredDirectoryId",
                table: "IndexingOptions",
                column: "MonitoredDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredDirectories_DirectoryId",
                table: "MonitoredDirectories",
                column: "DirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FileId",
                table: "Tags",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_MeanId",
                table: "Tags",
                column: "MeanId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_WordId",
                table: "Tags",
                column: "WordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexingOptions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "MonitoredDirectories");

            migrationBuilder.DropIndex(
                name: "IX_Files_DirectoryId_Name",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Directories_Path",
                table: "Directories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Words",
                table: "Words");

            migrationBuilder.RenameTable(
                name: "Words",
                newName: "Word");

            migrationBuilder.RenameIndex(
                name: "IX_Words_Text",
                table: "Word",
                newName: "IX_Word_Text");

            migrationBuilder.AddColumn<bool>(
                name: "IsTracked",
                table: "Directories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Word",
                table: "Word",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Texts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeanId = table.Column<int>(type: "INTEGER", nullable: false),
                    WordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Texts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Texts_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Texts_Means_MeanId",
                        column: x => x.MeanId,
                        principalTable: "Means",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Texts_Word_WordId",
                        column: x => x.WordId,
                        principalTable: "Word",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_DirectoryId",
                table: "Files",
                column: "DirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Texts_FileId",
                table: "Texts",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Texts_MeanId",
                table: "Texts",
                column: "MeanId");

            migrationBuilder.CreateIndex(
                name: "IX_Texts_WordId",
                table: "Texts",
                column: "WordId");
        }
    }
}

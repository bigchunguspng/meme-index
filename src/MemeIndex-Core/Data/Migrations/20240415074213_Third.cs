using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemeIndexCore.Migrations
{
    /// <inheritdoc />
    public partial class Third : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Representation",
                table: "Texts");

            migrationBuilder.AddColumn<int>(
                name: "WordId",
                table: "Texts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Word",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Word", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Texts_WordId",
                table: "Texts",
                column: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_Word_Text",
                table: "Word",
                column: "Text",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Texts_Word_WordId",
                table: "Texts",
                column: "WordId",
                principalTable: "Word",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Texts_Word_WordId",
                table: "Texts");

            migrationBuilder.DropTable(
                name: "Word");

            migrationBuilder.DropIndex(
                name: "IX_Texts_WordId",
                table: "Texts");

            migrationBuilder.DropColumn(
                name: "WordId",
                table: "Texts");

            migrationBuilder.AddColumn<string>(
                name: "Representation",
                table: "Texts",
                type: "TEXT",
                nullable: true);
        }
    }
}

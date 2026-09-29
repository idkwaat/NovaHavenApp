using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaHaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WikiMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiMedia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WikiDraftMedia",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiDraftMedia", x => new { x.ArticleId, x.MediaId });
                    table.ForeignKey(
                        name: "FK_WikiDraftMedia_WikiArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "WikiArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WikiDraftMedia_WikiMedia_MediaId",
                        column: x => x.MediaId,
                        principalTable: "WikiMedia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WikiRevisionMedia",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiRevisionMedia", x => new { x.RevisionId, x.MediaId });
                    table.ForeignKey(
                        name: "FK_WikiRevisionMedia_WikiArticleRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "WikiArticleRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WikiRevisionMedia_WikiMedia_MediaId",
                        column: x => x.MediaId,
                        principalTable: "WikiMedia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WikiDraftMedia_MediaId",
                table: "WikiDraftMedia",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiMedia_Sha256",
                table: "WikiMedia",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_WikiMedia_StorageKey",
                table: "WikiMedia",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiRevisionMedia_MediaId",
                table: "WikiRevisionMedia",
                column: "MediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WikiDraftMedia");

            migrationBuilder.DropTable(
                name: "WikiRevisionMedia");

            migrationBuilder.DropTable(
                name: "WikiMedia");
        }
    }
}

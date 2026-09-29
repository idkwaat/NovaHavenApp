using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaHaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "CatalogItemRevisions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CatalogRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WasPublished = table.Column<bool>(type: "bit", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRecipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRecipeDraftComponents",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRecipeDraftComponents", x => new { x.RecipeId, x.CatalogItemId, x.Role });
                    table.ForeignKey(
                        name: "FK_CatalogRecipeDraftComponents_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogRecipeDraftComponents_CatalogRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "CatalogRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRecipeRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRecipeRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogRecipeRevisions_CatalogRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "CatalogRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRecipeRevisionComponents",
                columns: table => new
                {
                    RecipeRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogItemRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRecipeRevisionComponents", x => new { x.RecipeRevisionId, x.CatalogItemRevisionId, x.Role });
                    table.ForeignKey(
                        name: "FK_CatalogRecipeRevisionComponents_CatalogItemRevisions_CatalogItemRevisionId",
                        column: x => x.CatalogItemRevisionId,
                        principalTable: "CatalogItemRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogRecipeRevisionComponents_CatalogRecipeRevisions_RecipeRevisionId",
                        column: x => x.RecipeRevisionId,
                        principalTable: "CatalogRecipeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogRecipeDraftComponents_CatalogItemId",
                table: "CatalogRecipeDraftComponents",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogRecipeRevisionComponents_CatalogItemRevisionId",
                table: "CatalogRecipeRevisionComponents",
                column: "CatalogItemRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogRecipeRevisions_RecipeId_Number",
                table: "CatalogRecipeRevisions",
                columns: new[] { "RecipeId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogRecipes_Slug",
                table: "CatalogRecipes",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogRecipeDraftComponents");

            migrationBuilder.DropTable(
                name: "CatalogRecipeRevisionComponents");

            migrationBuilder.DropTable(
                name: "CatalogRecipeRevisions");

            migrationBuilder.DropTable(
                name: "CatalogRecipes");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "CatalogItemRevisions");
        }
    }
}

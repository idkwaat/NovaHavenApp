using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaHaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DraftName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_KnowledgeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDraftLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetCatalogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDraftLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeDraftLinks_CatalogItems_TargetCatalogItemId",
                        column: x => x.TargetCatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeDraftLinks_KnowledgeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeDraftLinks_KnowledgeEntries_TargetEntryId",
                        column: x => x.TargetEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNpcProfiles",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PortraitUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNpcProfiles", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_KnowledgeNpcProfiles_KnowledgeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeNpcProfiles_KnowledgeEntries_LocationEntryId",
                        column: x => x.LocationEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQuestDefinitions",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    GiverNpcEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RewardDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestDefinitions", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitions_KnowledgeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitions_KnowledgeEntries_GiverNpcEntryId",
                        column: x => x.GiverNpcEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitions_KnowledgeEntries_LocationEntryId",
                        column: x => x.LocationEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQuestSteps",
                columns: table => new
                {
                    QuestEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestSteps", x => new { x.QuestEntryId, x.Position });
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestSteps_KnowledgeEntries_QuestEntryId",
                        column: x => x.QuestEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisions_KnowledgeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeSeasonDefinitions",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeSeasonDefinitions", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_KnowledgeSeasonDefinitions_KnowledgeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeWorldLocations",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MapImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeWorldLocations", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_KnowledgeWorldLocations_KnowledgeEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNpcProfileRevisions",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PortraitUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNpcProfileRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeNpcProfileRevisions_KnowledgeRevisions_LocationRevisionId",
                        column: x => x.LocationRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeNpcProfileRevisions_KnowledgeRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQuestDefinitionRevisions",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    GiverNpcRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RewardDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestDefinitionRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitionRevisions_KnowledgeRevisions_GiverNpcRevisionId",
                        column: x => x.GiverNpcRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitionRevisions_KnowledgeRevisions_LocationRevisionId",
                        column: x => x.LocationRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitionRevisions_KnowledgeRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQuestStepRevisions",
                columns: table => new
                {
                    QuestRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestStepRevisions", x => new { x.QuestRevisionId, x.Position });
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestStepRevisions_KnowledgeRevisions_QuestRevisionId",
                        column: x => x.QuestRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRevisionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetCatalogItemRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetSlug = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRevisionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisionLinks_CatalogItemRevisions_TargetCatalogItemRevisionId",
                        column: x => x.TargetCatalogItemRevisionId,
                        principalTable: "CatalogItemRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisionLinks_KnowledgeRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisionLinks_KnowledgeRevisions_TargetRevisionId",
                        column: x => x.TargetRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeSeasonDefinitionRevisions",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeSeasonDefinitionRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeSeasonDefinitionRevisions_KnowledgeRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeWorldLocationRevisions",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MapImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeWorldLocationRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeWorldLocationRevisions_KnowledgeRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDraftLinks_EntryId_SortOrder",
                table: "KnowledgeDraftLinks",
                columns: new[] { "EntryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDraftLinks_TargetCatalogItemId",
                table: "KnowledgeDraftLinks",
                column: "TargetCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDraftLinks_TargetEntryId",
                table: "KnowledgeDraftLinks",
                column: "TargetEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeEntries_Slug",
                table: "KnowledgeEntries",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNpcProfileRevisions_LocationRevisionId",
                table: "KnowledgeNpcProfileRevisions",
                column: "LocationRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNpcProfiles_LocationEntryId",
                table: "KnowledgeNpcProfiles",
                column: "LocationEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQuestDefinitionRevisions_GiverNpcRevisionId",
                table: "KnowledgeQuestDefinitionRevisions",
                column: "GiverNpcRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQuestDefinitionRevisions_LocationRevisionId",
                table: "KnowledgeQuestDefinitionRevisions",
                column: "LocationRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQuestDefinitions_GiverNpcEntryId",
                table: "KnowledgeQuestDefinitions",
                column: "GiverNpcEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQuestDefinitions_LocationEntryId",
                table: "KnowledgeQuestDefinitions",
                column: "LocationEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRevisionLinks_RevisionId_SortOrder",
                table: "KnowledgeRevisionLinks",
                columns: new[] { "RevisionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRevisionLinks_TargetCatalogItemRevisionId",
                table: "KnowledgeRevisionLinks",
                column: "TargetCatalogItemRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRevisionLinks_TargetRevisionId",
                table: "KnowledgeRevisionLinks",
                column: "TargetRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRevisions_EntryId_Number",
                table: "KnowledgeRevisions",
                columns: new[] { "EntryId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeDraftLinks");

            migrationBuilder.DropTable(
                name: "KnowledgeNpcProfileRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeNpcProfiles");

            migrationBuilder.DropTable(
                name: "KnowledgeQuestDefinitionRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeQuestDefinitions");

            migrationBuilder.DropTable(
                name: "KnowledgeQuestStepRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeQuestSteps");

            migrationBuilder.DropTable(
                name: "KnowledgeRevisionLinks");

            migrationBuilder.DropTable(
                name: "KnowledgeSeasonDefinitionRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeSeasonDefinitions");

            migrationBuilder.DropTable(
                name: "KnowledgeWorldLocationRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeWorldLocations");

            migrationBuilder.DropTable(
                name: "KnowledgeRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeEntries");
        }
    }
}

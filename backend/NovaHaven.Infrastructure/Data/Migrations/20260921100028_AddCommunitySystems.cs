using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaHaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunitySystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityRecords",
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
                    DraftStartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DraftEndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DraftLocationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DraftCapacity = table.Column<int>(type: "int", nullable: true),
                    DraftRegistrationOpen = table.Column<bool>(type: "bit", nullable: false),
                    DraftMotto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DraftDiscordUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DraftHandle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DraftBio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DraftAvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DraftOwnerDisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DraftGalleryMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DraftLeaderboardCategory = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DraftSeasonEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityRecords_KnowledgeEntries_DraftLocationEntryId",
                        column: x => x.DraftLocationEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityRecords_KnowledgeEntries_DraftSeasonEntryId",
                        column: x => x.DraftSeasonEntryId,
                        principalTable: "KnowledgeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityEventRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityEventRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityEventRegistrations_CommunityRecords_EventRecordId",
                        column: x => x.EventRecordId,
                        principalTable: "CommunityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityLeaderboardDraftRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityLeaderboardDraftRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityLeaderboardDraftRows_CommunityRecords_RecordId",
                        column: x => x.RecordId,
                        principalTable: "CommunityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityRecordRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LocationRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    RegistrationOpen = table.Column<bool>(type: "bit", nullable: false),
                    Motto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DiscordUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerDisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GalleryMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderboardCategory = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SeasonRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRecordRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityRecordRevisions_CommunityRecords_RecordId",
                        column: x => x.RecordId,
                        principalTable: "CommunityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityRecordRevisions_KnowledgeRevisions_LocationRevisionId",
                        column: x => x.LocationRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityRecordRevisions_KnowledgeRevisions_SeasonRevisionId",
                        column: x => x.SeasonRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityLeaderboardRevisionRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityLeaderboardRevisionRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityLeaderboardRevisionRows_CommunityRecordRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "CommunityRecordRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityEventRegistrations_EventRecordId_DisplayName",
                table: "CommunityEventRegistrations",
                columns: new[] { "EventRecordId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityLeaderboardDraftRows_RecordId_Rank",
                table: "CommunityLeaderboardDraftRows",
                columns: new[] { "RecordId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityLeaderboardRevisionRows_RevisionId_Rank",
                table: "CommunityLeaderboardRevisionRows",
                columns: new[] { "RevisionId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRecordRevisions_LocationRevisionId",
                table: "CommunityRecordRevisions",
                column: "LocationRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRecordRevisions_RecordId_Number",
                table: "CommunityRecordRevisions",
                columns: new[] { "RecordId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRecordRevisions_SeasonRevisionId",
                table: "CommunityRecordRevisions",
                column: "SeasonRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRecords_DraftLocationEntryId",
                table: "CommunityRecords",
                column: "DraftLocationEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRecords_DraftSeasonEntryId",
                table: "CommunityRecords",
                column: "DraftSeasonEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRecords_Slug",
                table: "CommunityRecords",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityEventRegistrations");

            migrationBuilder.DropTable(
                name: "CommunityLeaderboardDraftRows");

            migrationBuilder.DropTable(
                name: "CommunityLeaderboardRevisionRows");

            migrationBuilder.DropTable(
                name: "CommunityRecordRevisions");

            migrationBuilder.DropTable(
                name: "CommunityRecords");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaHaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardsCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommerceOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DraftKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DraftDisplayPrice = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DraftProviderProductCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("PK_CommerceOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DraftKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DraftDeliveryDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExternalAcknowledgementRequired = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_RewardDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommerceOfferRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayPrice = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProviderProductCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DefinitionOnly = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceOfferRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommerceOfferRevisions_CommerceOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "CommerceOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RewardDefinitionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeliveryDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExternalAcknowledgementRequired = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardDefinitionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardDefinitionRevisions_RewardDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "RewardDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOfferRevisions_OfferId_Number",
                table: "CommerceOfferRevisions",
                columns: new[] { "OfferId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOffers_Slug",
                table: "CommerceOffers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RewardDefinitionRevisions_DefinitionId_Number",
                table: "RewardDefinitionRevisions",
                columns: new[] { "DefinitionId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RewardDefinitions_Slug",
                table: "RewardDefinitions",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommerceOfferRevisions");

            migrationBuilder.DropTable(
                name: "RewardDefinitionRevisions");

            migrationBuilder.DropTable(
                name: "CommerceOffers");

            migrationBuilder.DropTable(
                name: "RewardDefinitions");
        }
    }
}

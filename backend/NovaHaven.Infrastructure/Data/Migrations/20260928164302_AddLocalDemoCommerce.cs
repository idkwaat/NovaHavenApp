using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaHaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalDemoCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DraftIsPurchasable",
                table: "CommerceOffers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "DraftPriceMinorUnits",
                table: "CommerceOffers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchasable",
                table: "CommerceOfferRevisions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PriceMinorUnits",
                table: "CommerceOfferRevisions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommerceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceOrders", x => x.Id);
                    table.CheckConstraint("CK_CommerceOrders_TotalMinorUnits", "[TotalMinorUnits] > 0");
                });

            migrationBuilder.CreateTable(
                name: "CommerceOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    OfferSlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OfferName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    LineTotalMinorUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceOrderLines", x => x.Id);
                    table.CheckConstraint("CK_CommerceOrderLines_PositiveValues", "[Quantity] BETWEEN 1 AND 99 AND [UnitPriceMinorUnits] > 0 AND [LineTotalMinorUnits] > 0");
                    table.ForeignKey(
                        name: "FK_CommerceOrderLines_CommerceOfferRevisions_OfferRevisionId",
                        column: x => x.OfferRevisionId,
                        principalTable: "CommerceOfferRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommerceOrderLines_CommerceOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "CommerceOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommerceOrderLines_CommerceOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CommerceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommercePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AmountMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercePayments", x => x.Id);
                    table.CheckConstraint("CK_CommercePayments_PositiveAmount", "[AmountMinorUnits] > 0");
                    table.ForeignKey(
                        name: "FK_CommercePayments_CommerceOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CommerceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOrderLines_OfferId",
                table: "CommerceOrderLines",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOrderLines_OfferRevisionId",
                table: "CommerceOrderLines",
                column: "OfferRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOrderLines_OrderId_OfferId",
                table: "CommerceOrderLines",
                columns: new[] { "OrderId", "OfferId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOrders_IdempotencyKey",
                table: "CommerceOrders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommerceOrders_OrderNumber",
                table: "CommerceOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommercePayments_OrderId",
                table: "CommercePayments",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommerceOrderLines");

            migrationBuilder.DropTable(
                name: "CommercePayments");

            migrationBuilder.DropTable(
                name: "CommerceOrders");

            migrationBuilder.DropColumn(
                name: "DraftIsPurchasable",
                table: "CommerceOffers");

            migrationBuilder.DropColumn(
                name: "DraftPriceMinorUnits",
                table: "CommerceOffers");

            migrationBuilder.DropColumn(
                name: "IsPurchasable",
                table: "CommerceOfferRevisions");

            migrationBuilder.DropColumn(
                name: "PriceMinorUnits",
                table: "CommerceOfferRevisions");
        }
    }
}

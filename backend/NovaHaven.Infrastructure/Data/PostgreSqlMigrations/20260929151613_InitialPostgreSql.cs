using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NovaHaven.Infrastructure.Data.PostgreSqlMigrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    DraftKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRecipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommerceOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    DraftKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DraftDisplayPrice = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DraftProviderProductCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DraftIsPurchasable = table.Column<bool>(type: "boolean", nullable: false),
                    DraftPriceMinorUnits = table.Column<long>(type: "bigint", nullable: true),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommerceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceOrders", x => x.Id);
                    table.CheckConstraint("CK_CommerceOrders_TotalMinorUnits", "\"TotalMinorUnits\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "IntegrationCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Owner = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SafeMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCapabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftTitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    DraftKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DraftDeliveryDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExternalAcknowledgementRequired = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WikiAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WikiCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WikiMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiMedia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WikiTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EndpointHash = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    P256dh = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Auth = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Href = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItemRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogItemRevisions_CatalogItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRecipeDraftComponents",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "CommerceOfferRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayPrice = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProviderProductCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsPurchasable = table.Column<bool>(type: "boolean", nullable: false),
                    PriceMinorUnits = table.Column<long>(type: "bigint", nullable: true),
                    DefinitionOnly = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "CommercePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AmountMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercePayments", x => x.Id);
                    table.CheckConstraint("CK_CommercePayments_PositiveAmount", "\"AmountMinorUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_CommercePayments_CommerceOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CommerceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    DraftStartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DraftEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DraftLocationEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DraftCapacity = table.Column<int>(type: "integer", nullable: true),
                    DraftRegistrationOpen = table.Column<bool>(type: "boolean", nullable: false),
                    DraftMotto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DraftDiscordUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftHandle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DraftBio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DraftAvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DraftOwnerDisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftGalleryMarkdown = table.Column<string>(type: "text", nullable: false),
                    DraftLeaderboardCategory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftSeasonEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "KnowledgeDraftLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCatalogItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
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
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocationEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PortraitUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    GiverNpcEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    RewardDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
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
                    QuestEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false)
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
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Theme = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
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
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    MapImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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
                name: "RewardDefinitionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExternalAcknowledgementRequired = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "WikiArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftTitle = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftSummary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DraftMarkdown = table.Column<string>(type: "text", nullable: false),
                    DraftCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LatestRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WikiArticles_WikiCategories_DraftCategoryId",
                        column: x => x.DraftCategoryId,
                        principalTable: "WikiCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRecipeRevisionComponents",
                columns: table => new
                {
                    RecipeRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogItemRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRecipeRevisionComponents", x => new { x.RecipeRevisionId, x.CatalogItemRevisionId, x.Role });
                    table.ForeignKey(
                        name: "FK_CatalogRecipeRevisionComponents_CatalogItemRevisions_Catalo~",
                        column: x => x.CatalogItemRevisionId,
                        principalTable: "CatalogItemRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogRecipeRevisionComponents_CatalogRecipeRevisions_Reci~",
                        column: x => x.RecipeRevisionId,
                        principalTable: "CatalogRecipeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommerceOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    OfferSlug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OfferName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    LineTotalMinorUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceOrderLines", x => x.Id);
                    table.CheckConstraint("CK_CommerceOrderLines_PositiveValues", "\"Quantity\" BETWEEN 1 AND 99 AND \"UnitPriceMinorUnits\" > 0 AND \"LineTotalMinorUnits\" > 0");
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
                name: "CommunityEventRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LocationRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    RegistrationOpen = table.Column<bool>(type: "boolean", nullable: false),
                    Motto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DiscordUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Handle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OwnerDisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    GalleryMarkdown = table.Column<string>(type: "text", nullable: false),
                    LeaderboardCategory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SeasonRevisionId = table.Column<Guid>(type: "uuid", nullable: true)
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
                        name: "FK_CommunityRecordRevisions_KnowledgeRevisions_LocationRevisio~",
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
                name: "KnowledgeNpcProfileRevisions",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocationRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PortraitUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNpcProfileRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeNpcProfileRevisions_KnowledgeRevisions_LocationRev~",
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
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    GiverNpcRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RewardDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestDefinitionRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitionRevisions_KnowledgeRevisions_GiverN~",
                        column: x => x.GiverNpcRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitionRevisions_KnowledgeRevisions_Locati~",
                        column: x => x.LocationRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestDefinitionRevisions_KnowledgeRevisions_Revisi~",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQuestStepRevisions",
                columns: table => new
                {
                    QuestRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestStepRevisions", x => new { x.QuestRevisionId, x.Position });
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestStepRevisions_KnowledgeRevisions_QuestRevisio~",
                        column: x => x.QuestRevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRevisionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCatalogItemRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetSlug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRevisionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisionLinks_CatalogItemRevisions_TargetCatalogIt~",
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
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Theme = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeSeasonDefinitionRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeSeasonDefinitionRevisions_KnowledgeRevisions_Revis~",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeWorldLocationRevisions",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    MapImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeWorldLocationRevisions", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_KnowledgeWorldLocationRevisions_KnowledgeRevisions_Revision~",
                        column: x => x.RevisionId,
                        principalTable: "KnowledgeRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WikiArticleRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiArticleRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WikiArticleRevisions_WikiArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "WikiArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WikiArticleRevisions_WikiCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "WikiCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WikiDraftMedia",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
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
                name: "WikiDraftTags",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiDraftTags", x => new { x.ArticleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_WikiDraftTags_WikiArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "WikiArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WikiDraftTags_WikiTags_TagId",
                        column: x => x.TagId,
                        principalTable: "WikiTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityLeaderboardRevisionRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityLeaderboardRevisionRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityLeaderboardRevisionRows_CommunityRecordRevisions_R~",
                        column: x => x.RevisionId,
                        principalTable: "CommunityRecordRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WikiRevisionMedia",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "WikiRevisionTags",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiRevisionTags", x => new { x.RevisionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_WikiRevisionTags_WikiArticleRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "WikiArticleRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WikiRevisionTags_WikiTags_TagId",
                        column: x => x.TagId,
                        principalTable: "WikiTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemRevisions_ItemId_Number",
                table: "CatalogItemRevisions",
                columns: new[] { "ItemId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Slug",
                table: "CatalogItems",
                column: "Slug",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCapabilities_CapabilityKey",
                table: "IntegrationCapabilities",
                column: "CapabilityKey",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_Slug",
                table: "NewsPosts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_EndpointHash",
                table: "PushSubscriptions",
                column: "EndpointHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_CreatedAtUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_ReadAtUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "ReadAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticleRevisions_ArticleId_Number",
                table: "WikiArticleRevisions",
                columns: new[] { "ArticleId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticleRevisions_CategoryId",
                table: "WikiArticleRevisions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_DraftCategoryId",
                table: "WikiArticles",
                column: "DraftCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_Slug",
                table: "WikiArticles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiAuditEvents_EntityType_EntityId_OccurredAt",
                table: "WikiAuditEvents",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WikiAuditEvents_OccurredAt",
                table: "WikiAuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_WikiCategories_NormalizedName",
                table: "WikiCategories",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiCategories_Slug",
                table: "WikiCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiDraftMedia_MediaId",
                table: "WikiDraftMedia",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiDraftTags_TagId",
                table: "WikiDraftTags",
                column: "TagId");

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

            migrationBuilder.CreateIndex(
                name: "IX_WikiRevisionTags_TagId",
                table: "WikiRevisionTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiTags_NormalizedName",
                table: "WikiTags",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiTags_Slug",
                table: "WikiTags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CatalogRecipeDraftComponents");

            migrationBuilder.DropTable(
                name: "CatalogRecipeRevisionComponents");

            migrationBuilder.DropTable(
                name: "CommerceOrderLines");

            migrationBuilder.DropTable(
                name: "CommercePayments");

            migrationBuilder.DropTable(
                name: "CommunityEventRegistrations");

            migrationBuilder.DropTable(
                name: "CommunityLeaderboardDraftRows");

            migrationBuilder.DropTable(
                name: "CommunityLeaderboardRevisionRows");

            migrationBuilder.DropTable(
                name: "IntegrationCapabilities");

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
                name: "NewsPosts");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "RewardDefinitionRevisions");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "WikiAuditEvents");

            migrationBuilder.DropTable(
                name: "WikiDraftMedia");

            migrationBuilder.DropTable(
                name: "WikiDraftTags");

            migrationBuilder.DropTable(
                name: "WikiRevisionMedia");

            migrationBuilder.DropTable(
                name: "WikiRevisionTags");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CatalogRecipeRevisions");

            migrationBuilder.DropTable(
                name: "CommerceOfferRevisions");

            migrationBuilder.DropTable(
                name: "CommerceOrders");

            migrationBuilder.DropTable(
                name: "CommunityRecordRevisions");

            migrationBuilder.DropTable(
                name: "CatalogItemRevisions");

            migrationBuilder.DropTable(
                name: "RewardDefinitions");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "WikiMedia");

            migrationBuilder.DropTable(
                name: "WikiArticleRevisions");

            migrationBuilder.DropTable(
                name: "WikiTags");

            migrationBuilder.DropTable(
                name: "CatalogRecipes");

            migrationBuilder.DropTable(
                name: "CommerceOffers");

            migrationBuilder.DropTable(
                name: "CommunityRecords");

            migrationBuilder.DropTable(
                name: "KnowledgeRevisions");

            migrationBuilder.DropTable(
                name: "CatalogItems");

            migrationBuilder.DropTable(
                name: "WikiArticles");

            migrationBuilder.DropTable(
                name: "KnowledgeEntries");

            migrationBuilder.DropTable(
                name: "WikiCategories");
        }
    }
}

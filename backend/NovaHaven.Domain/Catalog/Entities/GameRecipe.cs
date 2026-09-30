namespace NovaHaven.Domain.Catalog.Entities;

public enum RecipeComponentRole
{
    Ingredient,
    Output
}

public sealed class GameRecipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string DraftName { get; set; } = "";
    public string DraftSummary { get; set; } = "";
    public string DraftMarkdown { get; set; } = "";
    public CatalogItemState State { get; set; } = CatalogItemState.Draft;
    public Guid? PublishedRevisionId { get; set; }
    public bool WasPublished { get; set; }
    public int LatestRevisionNumber { get; set; }
    public uint RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class GameRecipeDraftComponent
{
    public Guid RecipeId { get; set; }
    public Guid CatalogItemId { get; set; }
    public RecipeComponentRole Role { get; set; }
    public int Quantity { get; set; }
}

public sealed class GameRecipeRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Markdown { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid PublishedBy { get; set; }

    public static GameRecipeRevision FromDraft(GameRecipe recipe, Guid publisherId, DateTimeOffset now) => new()
    {
        RecipeId = recipe.Id,
        Number = checked(recipe.LatestRevisionNumber + 1),
        Name = recipe.DraftName,
        Summary = recipe.DraftSummary,
        Markdown = recipe.DraftMarkdown,
        PublishedAt = now,
        PublishedBy = publisherId
    };
}

public sealed class GameRecipeRevisionComponent
{
    public Guid RecipeRevisionId { get; set; }
    public Guid CatalogItemRevisionId { get; set; }
    public RecipeComponentRole Role { get; set; }
    public int Quantity { get; set; }
}

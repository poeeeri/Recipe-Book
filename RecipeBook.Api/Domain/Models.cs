namespace RecipeBook.Api.Domain;

public sealed class DatabaseModel
{
    public List<Product> Products { get; set; } = [];
    public List<Dish> Dishes { get; set; } = [];
}

public sealed class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = [];
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbs { get; set; }
    public string? Composition { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CookingState { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class Dish
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = [];
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbs { get; set; }
    public List<DishItem> Items { get; set; } = [];
    public decimal PortionSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = [];
    public List<string> AvailableFlags { get; set; } = [];
    public NutritionDraft NutritionDraft { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class DishItem
{
    public string ProductId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public sealed class NutritionDraft
{
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbs { get; set; }
}

public sealed class ProductRequest
{
    public string? Name { get; set; }
    public List<string>? Photos { get; set; }
    public decimal? Calories { get; set; }
    public decimal? Proteins { get; set; }
    public decimal? Fats { get; set; }
    public decimal? Carbs { get; set; }
    public string? Composition { get; set; }
    public string? Category { get; set; }
    public string? CookingState { get; set; }
    public List<string>? Flags { get; set; }
}

public sealed class DishRequest
{
    public string? Name { get; set; }
    public List<string>? Photos { get; set; }
    public decimal? Calories { get; set; }
    public decimal? Proteins { get; set; }
    public decimal? Fats { get; set; }
    public decimal? Carbs { get; set; }
    public List<DishItemRequest>? Items { get; set; }
    public decimal? PortionSize { get; set; }
    public string? Category { get; set; }
    public List<string>? Flags { get; set; }
}

public sealed class DishItemRequest
{
    public string? ProductId { get; set; }
    public decimal? Quantity { get; set; }
}

public sealed class ProductUsageInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class DishItemDetails
{
    public string ProductId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Product Product { get; set; } = new();
}

public sealed class DishDetails
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = [];
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbs { get; set; }
    public List<DishItemDetails> Items { get; set; } = [];
    public decimal PortionSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = [];
    public List<string> AvailableFlags { get; set; } = [];
    public NutritionDraft NutritionDraft { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class MetaResponse
{
    public IReadOnlyList<string> ProductCategories { get; init; } = RecipeConstants.ProductCategories;
    public IReadOnlyList<string> ProductCookingStates { get; init; } = RecipeConstants.ProductCookingStates;
    public IReadOnlyList<string> DishCategories { get; init; } = RecipeConstants.DishCategories;
    public IReadOnlyList<string> Flags { get; init; } = RecipeConstants.Flags;
}

public sealed class MacroResult
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
}

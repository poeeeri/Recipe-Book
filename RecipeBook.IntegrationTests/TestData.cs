using RecipeBook.Api.Domain;

namespace RecipeBook.IntegrationTests;

internal static class TestData
{
    public const string TomatoId = "product-tomato";
    public const string TofuId = "product-tofu";
    public const string FlourId = "product-flour";
    public const string SaladId = "dish-salad";

    public static ProductRequest ValidProductRequest(
        string? name = "Tomato",
        decimal calories = 20m,
        decimal proteins = 1m,
        decimal fats = 0.2m,
        decimal carbs = 3m,
        IReadOnlyCollection<string>? photos = null,
        string? category = null,
        string? cookingState = null,
        IReadOnlyCollection<string>? flags = null) =>
        new()
        {
            Name = name,
            Photos = photos?.ToList() ?? [],
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs,
            Composition = "Fresh product",
            Category = category ?? RecipeConstants.ProductCategories[2],
            CookingState = cookingState ?? RecipeConstants.ProductCookingStates[0],
            Flags = flags?.ToList() ?? [RecipeConstants.Flags[0], RecipeConstants.Flags[1], RecipeConstants.Flags[2]]
        };

    public static DishRequest ValidDishRequest(
        string? name = null,
        decimal portionSize = 250m,
        decimal quantity = 150m,
        string productId = TomatoId,
        IReadOnlyCollection<string>? flags = null) =>
        new()
        {
            Name = name ?? "Tomato bowl",
            Photos = [],
            PortionSize = portionSize,
            Category = RecipeConstants.DishCategories[4],
            Flags = flags?.ToList() ?? [RecipeConstants.Flags[0], RecipeConstants.Flags[1], RecipeConstants.Flags[2]],
            Items =
            [
                new DishItemRequest
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            ]
        };

    public static DatabaseModel SeededDatabase() =>
        new()
        {
            Products =
            [
                Product(
                    TomatoId,
                    "Tomato",
                    calories: 20m,
                    proteins: 1m,
                    fats: 0.2m,
                    carbs: 3m,
                    category: RecipeConstants.ProductCategories[2],
                    flags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1], RecipeConstants.Flags[2]]),
                Product(
                    TofuId,
                    "Tofu",
                    calories: 120m,
                    proteins: 12m,
                    fats: 7m,
                    carbs: 2m,
                    category: RecipeConstants.ProductCategories[6],
                    flags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1]]),
                Product(
                    FlourId,
                    "Wheat flour",
                    calories: 364m,
                    proteins: 10m,
                    fats: 1m,
                    carbs: 76m,
                    category: RecipeConstants.ProductCategories[5],
                    flags: [])
            ],
            Dishes =
            [
                Dish(
                    SaladId,
                    "Tomato tofu salad",
                    category: RecipeConstants.DishCategories[4],
                    items:
                    [
                        new DishItem { ProductId = TomatoId, Quantity = 100m },
                        new DishItem { ProductId = TofuId, Quantity = 50m }
                    ])
            ]
        };

    private static Product Product(
        string id,
        string name,
        decimal calories,
        decimal proteins,
        decimal fats,
        decimal carbs,
        string category,
        IReadOnlyCollection<string> flags) =>
        new()
        {
            Id = id,
            Name = name,
            Photos = [],
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs,
            Composition = name,
            Category = category,
            CookingState = RecipeConstants.ProductCookingStates[0],
            Flags = [.. flags],
            CreatedAt = DateTime.UtcNow
        };

    private static Dish Dish(
        string id,
        string name,
        string category,
        IReadOnlyCollection<DishItem> items) =>
        new()
        {
            Id = id,
            Name = name,
            Photos = [],
            Calories = 80m,
            Proteins = 7m,
            Fats = 3.7m,
            Carbs = 4m,
            PortionSize = 200m,
            Category = category,
            Flags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
            AvailableFlags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
            NutritionDraft = new NutritionDraft
            {
                Calories = 80m,
                Proteins = 7m,
                Fats = 3.7m,
                Carbs = 4m
            },
            Items = [.. items],
            CreatedAt = DateTime.UtcNow
        };
}

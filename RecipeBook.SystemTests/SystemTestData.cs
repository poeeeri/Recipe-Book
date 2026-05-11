using RecipeBook.Api.Domain;

namespace RecipeBook.SystemTests;

internal static class SystemTestData
{
    public const string TomatoId = "ui-product-tomato";
    public const string TofuId = "ui-product-tofu";
    public const string SaladId = "ui-dish-salad";
    public const string TomatoSoupId = "ui-dish-tomato-soup";

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
                    flags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1]])
            ],
            Dishes =
            [
                Dish(
                    SaladId,
                    "Tomato tofu salad",
                    category: RecipeConstants.DishCategories[4],
                    calories: 80m,
                    proteins: 7m,
                    fats: 3.7m,
                    carbs: 4m,
                    portionSize: 200m,
                    flags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
                    availableFlags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
                    items:
                    [
                        new DishItem { ProductId = TomatoId, Quantity = 100m },
                        new DishItem { ProductId = TofuId, Quantity = 50m }
                    ]),
                Dish(
                    TomatoSoupId,
                    "Tomato soup",
                    category: RecipeConstants.DishCategories[5],
                    calories: 40m,
                    proteins: 2m,
                    fats: 0.4m,
                    carbs: 6m,
                    portionSize: 200m,
                    flags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1], RecipeConstants.Flags[2]],
                    availableFlags: [RecipeConstants.Flags[0], RecipeConstants.Flags[1], RecipeConstants.Flags[2]],
                    items:
                    [
                        new DishItem { ProductId = TomatoId, Quantity = 200m }
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
        decimal calories,
        decimal proteins,
        decimal fats,
        decimal carbs,
        decimal portionSize,
        IReadOnlyCollection<string> flags,
        IReadOnlyCollection<string> availableFlags,
        IReadOnlyCollection<DishItem> items) =>
        new()
        {
            Id = id,
            Name = name,
            Photos = [],
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs,
            PortionSize = portionSize,
            Category = category,
            Flags = [.. flags],
            AvailableFlags = [.. availableFlags],
            NutritionDraft = new NutritionDraft
            {
                Calories = calories,
                Proteins = proteins,
                Fats = fats,
                Carbs = carbs
            },
            Items = [.. items],
            CreatedAt = DateTime.UtcNow
        };
}

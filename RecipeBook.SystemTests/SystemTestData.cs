using RecipeBook.Api.Domain;

namespace RecipeBook.SystemTests;

internal static class SystemTestData
{
    public const string TomatoId = "ui-product-tomato";
    public const string TofuId = "ui-product-tofu";
    public const string SaladId = "ui-dish-salad";

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
                new Dish
                {
                    Id = SaladId,
                    Name = "Tomato tofu salad",
                    Photos = [],
                    Calories = 80m,
                    Proteins = 7m,
                    Fats = 3.7m,
                    Carbs = 4m,
                    PortionSize = 200m,
                    Category = RecipeConstants.DishCategories[4],
                    Flags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
                    AvailableFlags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
                    NutritionDraft = new NutritionDraft
                    {
                        Calories = 80m,
                        Proteins = 7m,
                        Fats = 3.7m,
                        Carbs = 4m
                    },
                    Items =
                    [
                        new DishItem { ProductId = TomatoId, Quantity = 100m },
                        new DishItem { ProductId = TofuId, Quantity = 50m }
                    ],
                    CreatedAt = DateTime.UtcNow
                }
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
}

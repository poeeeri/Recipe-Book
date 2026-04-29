using RecipeBook.Api.Domain;
using Xunit;

namespace RecipeBook.UnitTests;

/// unit-тесты для автоматического расчета калорийности блюда
/// использованные техники:
/// 1. эквивалентное разбиение
/// 2. анализ граничных значений
public sealed class DishCalorieCalculationTests
{
    private readonly RecipeDomainService _domain = new();
    private readonly Dictionary<string, Product> _productsById;

    public DishCalorieCalculationTests()
    {
        var cucumber = CreateProduct(
            id: "p-cucumber",
            name: "Огурец",
            calories: 20m,
            proteins: 1m,
            fats: 0.2m,
            carbs: 3m,
            flags: ["Веган", "Без глютена", "Без сахара"]);

        var tofu = CreateProduct(
            id: "p-tofu",
            name: "Тофу",
            calories: 120m,
            proteins: 12m,
            fats: 7m,
            carbs: 2m,
            flags: ["Веган", "Без глютена"]);

        _productsById = new Dictionary<string, Product>
        {
            [cucumber.Id] = cucumber,
            [tofu.Id] = tofu
        };
    }

    /// эквивалентное разбиение:
    /// 1. блюдо из одного продукта
    /// 2. блюдо из нескольких продуктов
    /// 3. блюдо с дробным количеством продукта
    [Theory]
    [MemberData(nameof(ValidCalorieEquivalenceCases))]
    public void CalculateDishNutrition_ReturnsExpectedCalories_ForValidEquivalenceClasses(
        DishItem[] items,
        decimal expectedCalories)
    {
        var result = _domain.CalculateDishNutrition(items, _productsById);
        Assert.Equal(expectedCalories, result.Calories);
    }

    /// анализ граничных значений:
    /// 0.01 г, 100 г и крупное допустимое значение
    /// проверяется корректность формулы на границах
    [Theory]
    [InlineData(0.01, 0.00)]
    [InlineData(100, 20.00)]
    [InlineData(1000, 200.00)]
    public void CalculateDishNutrition_HandlesBoundaryQuantities(decimal quantity, decimal expectedCalories)
    {
        var result = _domain.CalculateDishNutrition(
            [new DishItem { ProductId = "p-cucumber", Quantity = quantity }],
            _productsById);

        Assert.Equal(expectedCalories, result.Calories);
    }

    /// Негативный сценарий для эквивалентного разбиения:
    /// указание айди несуществующего продукт приводит к ошибке
    [Fact]
    public void CalculateDishNutrition_Throws_WhenProductDoesNotExist()
    {
        var exception = Assert.Throws<RecipeValidationException>(() =>
            _domain.CalculateDishNutrition(
                [new DishItem { ProductId = "missing-product", Quantity = 100 }],
                _productsById));

        Assert.Contains("Продукт не найден", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> ValidCalorieEquivalenceCases()
    {
        yield return
        [
            new[]
            {
                new DishItem { ProductId = "p-cucumber", Quantity = 100m }
            },
            20.00m
        ];

        yield return
        [
            new[]
            {
                new DishItem { ProductId = "p-cucumber", Quantity = 150m },
                new DishItem { ProductId = "p-tofu", Quantity = 100m }
            },
            150.00m
        ];

        yield return
        [
            new[]
            {
                new DishItem { ProductId = "p-tofu", Quantity = 12.5m }
            },
            15.00m
        ];
    }

    private Product CreateProduct(
        string id,
        string name,
        decimal calories,
        decimal proteins,
        decimal fats,
        decimal carbs,
        IReadOnlyCollection<string> flags)
    {
        var product = _domain.NormalizeProduct(new ProductRequest
        {
            Name = name,
            Photos = [],
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs,
            Composition = name,
            Category = RecipeConstants.ProductCategories[2],
            CookingState = RecipeConstants.ProductCookingStates[0],
            Flags = [.. flags]
        });

        product.Id = id;
        return product;
    }
}

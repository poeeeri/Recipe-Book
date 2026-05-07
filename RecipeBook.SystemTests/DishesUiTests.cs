using Microsoft.Playwright;
using Xunit;

namespace RecipeBook.SystemTests;

/// <summary>
/// Системные UI-тесты формы блюд.
/// Данные покрывают эквивалентное разбиение и анализ граничных значений.
/// </summary>
public sealed class DishesUiTests
{
    /// <summary>
    /// Системный сценарий: пользователь вводит состав блюда, и черновик КБЖУ
    /// автоматически подставляется в поля формы.
    /// </summary>
    [Fact]
    public async Task DishForm_FillsNutritionInputs_FromDraft()
    {
        await using var host = await SystemTestHost.StartAsync(SystemTestData.SeededDatabase());
        var page = await host.NewPageAsync();

        await FillDishBaseAsync(page);
        await SelectDishItemAsync(page, rowIndex: 0, productName: "Tomato", quantity: "150");
        await page.Locator("#add-dish-item").ClickAsync();
        await SelectDishItemAsync(page, rowIndex: 1, productName: "Tofu", quantity: "100");

        await Assertions.Expect(page.Locator("#dish-form input[name=calories]")).ToHaveValueAsync("150.00");
    }

    /// <summary>
    /// Анализ граничных значений для размера порции: значения меньше или равные 0
    /// отклоняются, первое значение выше границы принимается.
    /// </summary>
    [Theory]
    [InlineData("-0.01", false)]
    [InlineData("0", false)]
    [InlineData("0.01", true)]
    public async Task DishPortionSizeInput_ValidatesPositiveBoundary(string portionSize, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync(SystemTestData.SeededDatabase());
        var page = await host.NewPageAsync();

        await page.Locator("#dish-form input[name=portionSize]").FillAsync(portionSize);
        var isValid = await page.Locator("#dish-form input[name=portionSize]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Анализ граничных значений для количества ингредиента: значения меньше или равные 0
    /// отклоняются, первое значение выше границы принимается.
    /// </summary>
    [Theory]
    [InlineData("-0.01", false)]
    [InlineData("0", false)]
    [InlineData("0.01", true)]
    public async Task DishItemQuantityInput_ValidatesPositiveBoundary(string quantity, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync(SystemTestData.SeededDatabase());
        var page = await host.NewPageAsync();

        await page.Locator("#dish-items input[name=quantity]").FillAsync(quantity);
        var isValid = await page.Locator("#dish-items input[name=quantity]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Эквивалентное разбиение для фильтрации блюд: поиск по названию оставляет
    /// видимой только подходящую карточку.
    /// </summary>
    [Fact]
    public async Task DishSearch_ShowsMatchingDish()
    {
        await using var host = await SystemTestHost.StartAsync(SystemTestData.SeededDatabase());
        var page = await host.NewPageAsync();

        await page.Locator("#dish-search").FillAsync("tofu");

        await Assertions.Expect(page.Locator("#dish-list .card").Filter(new LocatorFilterOptions { HasTextString = "Tomato tofu salad" })).ToBeVisibleAsync();
    }

    private static async Task FillDishBaseAsync(IPage page)
    {
        await page.Locator("#dish-form input[name=name]").FillAsync("Tomato tofu bowl");
        await page.Locator("#dish-form input[name=portionSize]").FillAsync("250");
        await page.Locator("#dish-form select[name=category]").SelectOptionAsync(new SelectOptionValue { Index = 1 });
    }

    private static async Task SelectDishItemAsync(IPage page, int rowIndex, string productName, string quantity)
    {
        var row = page.Locator("#dish-items .dish-item-row").Nth(rowIndex);
        await row.Locator("select[name=productId]").SelectOptionAsync(new[] { productName });
        await row.Locator("input[name=quantity]").FillAsync(quantity);
    }
}

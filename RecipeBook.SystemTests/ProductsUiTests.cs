using Microsoft.Playwright;
using Xunit;

namespace RecipeBook.SystemTests;

/// <summary>
/// Системные UI-тесты формы продуктов.
/// Данные покрывают эквивалентное разбиение и анализ граничных значений.
/// </summary>
public sealed class ProductsUiTests
{
    /// <summary>
    /// Системный сценарий: пользователь создает продукт через UI и видит карточку продукта.
    /// </summary>
    [Fact]
    public async Task ProductForm_CreatesProductCard_ForValidInput()
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        await FillValidProductAsync(page, "Milk", calories: "64", proteins: "3.2", fats: "3.6", carbs: "4.8");
        await page.Locator("#product-form button[type=submit]").ClickAsync();

        await Expect(page.Locator("#product-list .card").Filter(new LocatorFilterOptions { HasTextString = "Milk" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Анализ граничных значений для названия: значения короче 2 символов
    /// не проходят HTML-валидацию, значения от 2 символов проходят.
    /// </summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("A", false)]
    [InlineData("AB", true)]
    public async Task ProductNameInput_ValidatesLengthBoundary(string name, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        await page.Locator("#product-form input[name=name]").FillAsync(name);
        var isValid = await page.Locator("#product-form input[name=name]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Анализ граничных значений для калорийности: значение ниже 0 отклоняется,
    /// точная нижняя граница и значение выше нее принимаются.
    /// </summary>
    [Theory]
    [InlineData("-0.01", false)]
    [InlineData("0", true)]
    [InlineData("0.01", true)]
    public async Task ProductCaloriesInput_ValidatesMinimumBoundary(string calories, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        await page.Locator("#product-form input[name=calories]").FillAsync(calories);
        var isValid = await page.Locator("#product-form input[name=calories]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Эквивалентное разбиение для обязательных полей продукта:
    /// пустое поле не проходит HTML-валидацию.
    /// </summary>
    [Theory]
    [InlineData("name")]
    [InlineData("calories")]
    [InlineData("proteins")]
    [InlineData("fats")]
    [InlineData("carbs")]
    public async Task ProductRequiredInputs_RejectEmptyValue(string inputName)
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        var isValid = await page.Locator($"#product-form input[name={inputName}]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.False(isValid);
    }

    private static async Task FillValidProductAsync(IPage page, string name, string calories, string proteins, string fats, string carbs)
    {
        await page.Locator("#product-form input[name=name]").FillAsync(name);
        await page.Locator("#product-form input[name=calories]").FillAsync(calories);
        await page.Locator("#product-form input[name=proteins]").FillAsync(proteins);
        await page.Locator("#product-form input[name=fats]").FillAsync(fats);
        await page.Locator("#product-form input[name=carbs]").FillAsync(carbs);
        await page.Locator("#product-form select[name=category]").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("#product-form select[name=cookingState]").SelectOptionAsync(new SelectOptionValue { Index = 1 });
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
}

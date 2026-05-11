using Microsoft.Playwright;
using RecipeBook.Api.Domain;
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
    [InlineData("ABC", true)]
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
    /// Анализ граничных значений для белков: допустимый диапазон в UI от 0 до 100.
    /// </summary>
    [Theory]
    [InlineData("-0.01", false)]
    [InlineData("0", true)]
    [InlineData("0.01", true)]
    [InlineData("100", true)]
    [InlineData("100.01", false)]
    public async Task ProductProteinsInput_ValidatesBoundary(string proteins, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        await page.Locator("#product-form input[name=proteins]").FillAsync(proteins);
        var isValid = await page.Locator("#product-form input[name=proteins]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Анализ граничных значений для жиров: допустимый диапазон в UI от 0 до 100.
    /// </summary>
    [Theory]
    [InlineData("-0.01", false)]
    [InlineData("0", true)]
    [InlineData("0.01", true)]
    [InlineData("100", true)]
    [InlineData("100.01", false)]
    public async Task ProductFatsInput_ValidatesBoundary(string fats, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        await page.Locator("#product-form input[name=fats]").FillAsync(fats);
        var isValid = await page.Locator("#product-form input[name=fats]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Анализ граничных значений для углеводов: допустимый диапазон в UI от 0 до 100.
    /// </summary>
    [Theory]
    [InlineData("-0.01", false)]
    [InlineData("0", true)]
    [InlineData("0.01", true)]
    [InlineData("100", true)]
    [InlineData("100.01", false)]
    public async Task ProductCarbsInput_ValidatesBoundary(string carbs, bool expectedValid)
    {
        await using var host = await SystemTestHost.StartAsync();
        var page = await host.NewPageAsync();

        await page.Locator("#product-form input[name=carbs]").FillAsync(carbs);
        var isValid = await page.Locator("#product-form input[name=carbs]").EvaluateAsync<bool>("input => input.checkValidity()");

        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Эквивалентное разбиение для обязательных полей продукта:
    /// невалидный класс "пустое обязательное поле" не проходит HTML-валидацию.
    /// Валидный класс заполненных обязательных полей проверяется сценарием создания продукта.
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

    /// <summary>
    /// Системный сценарий: фильтрация продуктов по категории оставляет видимыми
    /// только продукты выбранной категории.
    /// </summary>
    [Fact]
    public async Task ProductCategoryFilter_ShowsProductsFromSelectedCategory()
    {
        await using var host = await SystemTestHost.StartAsync(SystemTestData.SeededDatabase());
        var page = await host.NewPageAsync();

        await page.Locator("#product-filter-category").SelectOptionAsync([RecipeConstants.ProductCategories[2]]);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.Locator("#product-list .card")).ToHaveCountAsync(1);
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

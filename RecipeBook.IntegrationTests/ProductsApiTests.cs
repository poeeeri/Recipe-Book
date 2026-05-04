using System.Net;
using System.Net.Http.Json;
using RecipeBook.Api.Domain;
using Xunit;

namespace RecipeBook.IntegrationTests;

/// <summary>
/// Интеграционные API-тесты для продуктов.
/// Тестовые данные покрывают эквивалентное разбиение и анализ граничных значений.
/// </summary>
public sealed class ProductsApiTests
{
    /// <summary>
    /// Анализ граничных значений для длины названия продукта.
    /// Значения короче 2 символов отклоняются, значения от 2 символов принимаются.
    /// </summary>
    [Theory]
    [InlineData(null, HttpStatusCode.BadRequest)]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData("A", HttpStatusCode.BadRequest)]
    [InlineData("AB", HttpStatusCode.Created)]
    [InlineData("ABC", HttpStatusCode.Created)]
    public async Task CreateProduct_ValidatesNameLengthBoundary(string? name, HttpStatusCode expectedStatus)
    {
        await using var host = await ApiTestHost.StartAsync();

        var response = await host.CreateProductAsync(TestData.ValidProductRequest(name: name));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    /// <summary>
    /// Анализ граничных значений для числовых полей продукта.
    /// Нижняя граница равна 0, верхняя граница для полей БЖУ равна 100.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidProductNumericBoundaryCases))]
    public async Task CreateProduct_AcceptsValidNumericBoundaries(ProductRequest request)
    {
        await using var host = await ApiTestHost.StartAsync();

        var response = await host.CreateProductAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Анализ граничных значений: созданный продукт сохраняет точное граничное значение калорий.
    /// </summary>
    [Fact]
    public async Task CreateProduct_ReturnsCalories_WhenCaloriesAreOnBoundary()
    {
        await using var host = await ApiTestHost.StartAsync();

        var response = await host.CreateProductAsync(TestData.ValidProductRequest(calories: 0m));
        var product = await response.Content.ReadFromJsonAsync<Product>(ApiTestHost.JsonOptions);

        Assert.Equal(0m, product?.Calories);
    }

    /// <summary>
    /// Эквивалентное разбиение для невалидных данных продукта.
    /// Каждая строка содержит один невалидный класс, остальные поля остаются корректными.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidProductEquivalenceCases))]
    public async Task CreateProduct_RejectsInvalidEquivalenceClasses(ProductRequest request)
    {
        await using var host = await ApiTestHost.StartAsync();

        var response = await host.CreateProductAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Интеграция API: фильтрация продуктов по категории возвращает подходящий продукт.
    /// </summary>
    [Fact]
    public async Task GetProducts_FiltersByCategory()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.GetAsync($"/api/products?category={Uri.EscapeDataString(RecipeConstants.ProductCategories[2])}");
        var products = await response.Content.ReadFromJsonAsync<List<Product>>(ApiTestHost.JsonOptions);

        Assert.Equal(TestData.TomatoId, products?[0].Id);
    }

    /// <summary>
    /// Интеграция API: фильтрация продуктов по флагу возвращает только продукты с этим флагом.
    /// </summary>
    [Fact]
    public async Task GetProducts_FiltersByFlag()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.GetAsync($"/api/products?flags={Uri.EscapeDataString(RecipeConstants.Flags[0])}");
        var products = await response.Content.ReadFromJsonAsync<List<Product>>(ApiTestHost.JsonOptions);

        Assert.Equal(2, products?.Count);
    }

    /// <summary>
    /// Интеграция API: сортировка продуктов по калориям по убыванию возвращает самый калорийный продукт первым.
    /// </summary>
    [Fact]
    public async Task GetProducts_SortsByCaloriesDescending()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.GetAsync("/api/products?sort=calories&order=desc");
        var products = await response.Content.ReadFromJsonAsync<List<Product>>(ApiTestHost.JsonOptions);

        Assert.Equal(TestData.FlourId, products?[0].Id);
    }

    /// <summary>
    /// Интеграция API: удаление продукта, который используется в блюде, возвращает конфликт.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_ReturnsConflict_WhenProductIsUsedByDish()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.DeleteAsync($"/api/products/{TestData.TomatoId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// Интеграция API: успешный CRUD-сценарий продукта проходит через создание,
    /// чтение, обновление, удаление и повторное чтение удаленного ресурса.
    /// </summary>
    [Fact]
    public async Task ProductCrud_CompletesSuccessfulLifecycle()
    {
        await using var host = await ApiTestHost.StartAsync();

        var createResponse = await host.CreateProductAsync(TestData.ValidProductRequest(
            name: "Milk",
            calories: 64m,
            proteins: 3.2m,
            fats: 3.6m,
            carbs: 4.8m,
            category: RecipeConstants.ProductCategories[7],
            cookingState: RecipeConstants.ProductCookingStates[0],
            flags: [RecipeConstants.Flags[1]]));
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>(ApiTestHost.JsonOptions);
        var getResponse = await host.Client.GetAsync($"/api/products/{createdProduct?.Id}");
        var updateResponse = await host.Client.PutAsJsonAsync(
            $"/api/products/{createdProduct?.Id}",
            TestData.ValidProductRequest(
                name: "Updated milk",
                calories: 70m,
                proteins: 3.4m,
                fats: 4.0m,
                carbs: 5.1m,
                category: RecipeConstants.ProductCategories[7],
                cookingState: RecipeConstants.ProductCookingStates[0],
                flags: [RecipeConstants.Flags[1]]),
            ApiTestHost.JsonOptions);
        var deleteResponse = await host.Client.DeleteAsync($"/api/products/{createdProduct?.Id}");
        var getAfterDeleteResponse = await host.Client.GetAsync($"/api/products/{createdProduct?.Id}");

        Assert.Equal(
            (HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.NotFound),
            (createResponse.StatusCode, getResponse.StatusCode, updateResponse.StatusCode, deleteResponse.StatusCode, getAfterDeleteResponse.StatusCode));
    }

    /// <summary>
    /// Интеграция API: обновление пищевой ценности продукта пересчитывает черновик КБЖУ блюда.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_RecalculatesExistingDishDraftCalories()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        await host.Client.PutAsJsonAsync(
            $"/api/products/{TestData.TomatoId}",
            TestData.ValidProductRequest(name: "Tomato", calories: 40m, proteins: 2m, fats: 0.4m, carbs: 6m),
            ApiTestHost.JsonOptions);
        var dishResponse = await host.Client.GetAsync($"/api/dishes/{TestData.SaladId}");
        var dish = await dishResponse.Content.ReadFromJsonAsync<DishDetails>(ApiTestHost.JsonOptions);

        Assert.Equal(100m, dish?.NutritionDraft.Calories);
    }

    public static TheoryData<ProductRequest> ValidProductNumericBoundaryCases()
    {
        return new TheoryData<ProductRequest>
        {
            TestData.ValidProductRequest(calories: 0m, proteins: 0m, fats: 0m, carbs: 0m),
            TestData.ValidProductRequest(calories: 0.01m, proteins: 0.01m, fats: 0m, carbs: 0m),
            TestData.ValidProductRequest(calories: 500m, proteins: 100m, fats: 0m, carbs: 0m),
            TestData.ValidProductRequest(calories: 500m, proteins: 0m, fats: 100m, carbs: 0m),
            TestData.ValidProductRequest(calories: 500m, proteins: 0m, fats: 0m, carbs: 100m),
            TestData.ValidProductRequest(calories: 500m, proteins: 33.33m, fats: 33.33m, carbs: 33.34m)
        };
    }

    public static TheoryData<ProductRequest> InvalidProductEquivalenceCases()
    {
        var missingCookingState = TestData.ValidProductRequest();
        missingCookingState.CookingState = null;

        var emptyCookingState = TestData.ValidProductRequest();
        emptyCookingState.CookingState = "";

        return new TheoryData<ProductRequest>
        {
            TestData.ValidProductRequest(calories: -0.01m),
            TestData.ValidProductRequest(proteins: -0.01m),
            TestData.ValidProductRequest(fats: -0.01m),
            TestData.ValidProductRequest(carbs: -0.01m),
            TestData.ValidProductRequest(proteins: 40m, fats: 40m, carbs: 20.01m),
            TestData.ValidProductRequest(photos: ["1", "2", "3", "4", "5", "6"]),
            TestData.ValidProductRequest(category: "unsupported-category"),
            TestData.ValidProductRequest(cookingState: "unsupported-state"),
            missingCookingState,
            emptyCookingState,
            TestData.ValidProductRequest(flags: ["unsupported-flag"])
        };
    }
}

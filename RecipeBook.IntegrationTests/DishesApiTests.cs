using System.Net;
using System.Net.Http.Json;
using RecipeBook.Api.Domain;
using Xunit;

namespace RecipeBook.IntegrationTests;

/// <summary>
/// Интеграционные API-тесты для блюд.
/// Тестовые данные покрывают эквивалентное разбиение и анализ граничных значений.
/// </summary>
public sealed class DishesApiTests
{
    /// <summary>
    /// Интеграция API: корректное блюдо создается из сохраненных продуктов.
    /// </summary>
    [Fact]
    public async Task CreateDish_ReturnsCreated_ForValidDish()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.CreateDishAsync(CreateTomatoTofuDish());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Интеграция API: калорийность блюда рассчитывается по сохраненным данным продуктов.
    /// </summary>
    [Fact]
    public async Task CreateDish_ReturnsCalculatedCalories()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.CreateDishAsync(CreateTomatoTofuDish());
        var dish = await response.Content.ReadFromJsonAsync<DishDetails>(ApiTestHost.JsonOptions);

        Assert.Equal(150m, dish?.NutritionDraft.Calories);
    }

    /// <summary>
    /// Интеграция API: выбираются только флаги, доступные для всех ингредиентов.
    /// </summary>
    [Fact]
    public async Task CreateDish_ReturnsOnlyAvailableFlags()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.CreateDishAsync(CreateTomatoTofuDish());
        var dish = await response.Content.ReadFromJsonAsync<DishDetails>(ApiTestHost.JsonOptions);

        Assert.Equal(2, dish?.Flags.Count);
    }

    /// <summary>
    /// Анализ граничных значений для количества ингредиента и размера порции.
    /// Значения меньше или равные 0 отклоняются, значение 0.01 принимается.
    /// </summary>
    [Theory]
    [MemberData(nameof(DishPositiveNumberBoundaryCases))]
    public async Task CreateDish_ValidatesPositiveNumberBoundaries(DishRequest request, HttpStatusCode expectedStatus)
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.CreateDishAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    /// <summary>
    /// Эквивалентное разбиение для невалидного состава блюда.
    /// Каждая строка содержит один невалидный класс состава.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidDishCompositionEquivalenceCases))]
    public async Task CreateDish_RejectsInvalidCompositionEquivalenceClasses(DishRequest request)
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.CreateDishAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Анализ граничных значений для БЖУ на 100 г.
    /// Точная сумма 100 принимается, первое значение выше 100 отклоняется.
    /// </summary>
    [Theory]
    [InlineData(100, 0, 0, HttpStatusCode.Created)]
    [InlineData(100.01, 0, 0, HttpStatusCode.BadRequest)]
    [InlineData(33.33, 33.33, 33.34, HttpStatusCode.Created)]
    [InlineData(33.33, 33.33, 33.35, HttpStatusCode.BadRequest)]
    public async Task CreateDish_ValidatesBjuPer100gBoundary(
        decimal proteins,
        decimal fats,
        decimal carbs,
        HttpStatusCode expectedStatus)
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var request = TestData.ValidDishRequest(portionSize: 100m);
        request.Proteins = proteins;
        request.Fats = fats;
        request.Carbs = carbs;
        request.Calories = 500m;
        var response = await host.CreateDishAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    /// <summary>
    /// Интеграция API: поиск блюд по названию возвращает подходящее блюдо.
    /// </summary>
    [Fact]
    public async Task GetDishes_FiltersBySearch()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.GetAsync("/api/dishes?search=tofu");
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDetails>>(ApiTestHost.JsonOptions);

        Assert.Equal(TestData.SaladId, dishes?[0].Id);
    }

    /// <summary>
    /// Интеграция API: фильтрация блюд по категории возвращает подходящее блюдо.
    /// </summary>
    [Fact]
    public async Task GetDishes_FiltersByCategory()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.GetAsync($"/api/dishes?category={Uri.EscapeDataString(RecipeConstants.DishCategories[4])}");
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDetails>>(ApiTestHost.JsonOptions);

        Assert.Equal(TestData.SaladId, dishes?[0].Id);
    }

    /// <summary>
    /// Интеграция API: фильтрация блюд по флагу возвращает подходящее блюдо.
    /// </summary>
    [Fact]
    public async Task GetDishes_FiltersByFlag()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var response = await host.Client.GetAsync($"/api/dishes?flags={Uri.EscapeDataString(RecipeConstants.Flags[0])}");
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDetails>>(ApiTestHost.JsonOptions);

        Assert.Equal(TestData.SaladId, dishes?[0].Id);
    }

    /// <summary>
    /// Интеграция API: успешный CRUD-сценарий блюда проходит через создание,
    /// чтение, обновление, удаление и повторное чтение удаленного ресурса.
    /// </summary>
    [Fact]
    public async Task DishCrud_CompletesSuccessfulLifecycle()
    {
        await using var host = await ApiTestHost.StartAsync(TestData.SeededDatabase());

        var createResponse = await host.CreateDishAsync(new DishRequest
        {
            Name = "bowl",
            Photos = [],
            PortionSize = 250m,
            Category = RecipeConstants.DishCategories[4],
            Flags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
            Items =
            [
                new DishItemRequest { ProductId = TestData.TomatoId, Quantity = 150m },
                new DishItemRequest { ProductId = TestData.TofuId, Quantity = 100m }
            ]
        });
        var createdDish = await createResponse.Content.ReadFromJsonAsync<DishDetails>(ApiTestHost.JsonOptions);
        var getResponse = await host.Client.GetAsync($"/api/dishes/{createdDish?.Id}");
        var updateResponse = await host.Client.PutAsJsonAsync(
            $"/api/dishes/{createdDish?.Id}",
            new DishRequest
            {
                Name = "Updated tomato bowl",
                Photos = [],
                PortionSize = 300m,
                Category = RecipeConstants.DishCategories[4],
                Flags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1]],
                Items =
                [
                    new DishItemRequest { ProductId = TestData.TomatoId, Quantity = 200m },
                    new DishItemRequest { ProductId = TestData.TofuId, Quantity = 100m }
                ]
            },
            ApiTestHost.JsonOptions);
        var deleteResponse = await host.Client.DeleteAsync($"/api/dishes/{createdDish?.Id}");
        var getAfterDeleteResponse = await host.Client.GetAsync($"/api/dishes/{createdDish?.Id}");

        Assert.Equal(
            (HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.NotFound),
            (createResponse.StatusCode, getResponse.StatusCode, updateResponse.StatusCode, deleteResponse.StatusCode, getAfterDeleteResponse.StatusCode));
    }

    public static TheoryData<DishRequest, HttpStatusCode> DishPositiveNumberBoundaryCases()
    {
        return new TheoryData<DishRequest, HttpStatusCode>
        {
            { TestData.ValidDishRequest(quantity: -0.01m), HttpStatusCode.BadRequest },
            { TestData.ValidDishRequest(quantity: 0m), HttpStatusCode.BadRequest },
            { TestData.ValidDishRequest(quantity: 0.01m), HttpStatusCode.Created },
            { TestData.ValidDishRequest(portionSize: -0.01m), HttpStatusCode.BadRequest },
            { TestData.ValidDishRequest(portionSize: 0m), HttpStatusCode.BadRequest },
            { TestData.ValidDishRequest(portionSize: 0.01m, quantity: 0.01m), HttpStatusCode.Created }
        };
    }

    public static TheoryData<DishRequest> InvalidDishCompositionEquivalenceCases()
    {
        var missingItems = TestData.ValidDishRequest();
        missingItems.Items = null;

        var emptyItems = TestData.ValidDishRequest();
        emptyItems.Items = [];

        return new TheoryData<DishRequest>
        {
            missingItems,
            emptyItems,
            TestData.ValidDishRequest(productId: " "),
            TestData.ValidDishRequest(productId: "missing-product")
        };
    }

    private static DishRequest CreateTomatoTofuDish() =>
        new()
        {
            Name = "Tomato tofu bowl",
            Photos = [],
            PortionSize = 250m,
            Category = RecipeConstants.DishCategories[4],
            Flags = [RecipeConstants.Flags[0], RecipeConstants.Flags[1], RecipeConstants.Flags[2]],
            Items =
            [
                new DishItemRequest { ProductId = TestData.TomatoId, Quantity = 150m },
                new DishItemRequest { ProductId = TestData.TofuId, Quantity = 100m }
            ]
        };
}

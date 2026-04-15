using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Api.Domain;

await TestRunner.RunAsync();

internal static class TestRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task RunAsync()
    {
        var failures = 0;
        failures += await RunCaseAsync("продукт не соответствует требованиям, сумма БЖУ больше 100", TestProductBjuValidationAsync);
        failures += await RunCaseAsync("продукт не может иметь более, чем 5 фото", TestProductPhotoLimitAsync);
        failures += await RunCaseAsync("блюдо соответствует требованиям БЖУ по весу 100г, а не по размеру порции", TestDishBjuPer100gAsync);
        failures += await RunCaseAsync("макрос блюда использует первый макрос, и явная категория переопределяет его", TestMacroSelectionAsync);
        failures += await RunCaseAsync("блюдо пересчитывает доступные флаги на основе состава", TestDishNormalizationAsync);
        failures += await RunCaseAsync("фильтрация и сортировка продуктов работают", TestProductFilteringAsync);
        failures += await RunCaseAsync("фильтрация блюд работает", TestDishFilteringAsync);
        failures += await RunCaseAsync("API блокирует удаление продукта, используемого в блюдах", TestDeleteConflictApiAsync);
        failures += await RunCaseAsync("API создаёт блюдо с рассчитанным черновиком и категорией макросов", TestDishCreationApiAsync);

        if (failures > 0)
        {
            Environment.ExitCode = 1;
        }
    }

    private static async Task<int> RunCaseAsync(string name, Func<Task> testCase)
    {
        try
        {
            await testCase();
            Console.WriteLine($"PASS {name}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"FAIL {name}");
            Console.WriteLine(exception);
            return 1;
        }
    }

    private static Task TestProductBjuValidationAsync()
    {
        var domain = new RecipeDomainService();
        AssertThrows<RecipeValidationException>(() => domain.NormalizeProduct(new ProductRequest
        {
            Name = "тест",
            Photos = [],
            Calories = 100,
            Proteins = 40,
            Fats = 40,
            Carbs = 30,
            Composition = "состав",
            Category = RecipeConstants.ProductCategories[2],
            CookingState = RecipeConstants.ProductCookingStates[0],
            Flags = []
        }), "100");

        return Task.CompletedTask;
    }

    private static Task TestProductPhotoLimitAsync()
    {
        var domain = new RecipeDomainService();
        AssertThrows<RecipeValidationException>(() => domain.NormalizeProduct(new ProductRequest
        {
            Name = "Тест",
            Photos = ["1", "2", "3", "4", "5", "6"],
            Calories = 10,
            Proteins = 1,
            Fats = 1,
            Carbs = 1,
            Category = RecipeConstants.ProductCategories[2],
            CookingState = RecipeConstants.ProductCookingStates[0],
            Flags = []
        }), "5");

        return Task.CompletedTask;
    }

    private static Task TestDishBjuPer100gAsync()
    {
        var domain = new RecipeDomainService();
        var products = CreateProducts(domain);

        var validDish = domain.NormalizeDish(new DishRequest
        {
            Name = "летний салат",
            Photos = [],
            PortionSize = 300,
            Proteins = 120,
            Fats = 30,
            Carbs = 30,
            Calories = 300,
            Category = "Салат",
            Flags = ["Веган", "Без глютена"],
            Items =
            [
                new DishItemRequest { ProductId = products[0].Id, Quantity = 150 },
                new DishItemRequest { ProductId = products[1].Id, Quantity = 150 }
            ]
        }, products);

        AssertEqual(120m, validDish.Proteins, "dish proteins");

        AssertThrows<RecipeValidationException>(() => domain.NormalizeDish(new DishRequest
        {
            Name = "плотный салат",
            Photos = [],
            PortionSize = 100,
            Proteins = 50,
            Fats = 30,
            Carbs = 25,
            Calories = 300,
            Category = "Салат",
            Flags = ["Веган", "Без глютена"],
            Items =
            [
                new DishItemRequest { ProductId = products[0].Id, Quantity = 50 },
                new DishItemRequest { ProductId = products[1].Id, Quantity = 50 }
            ]
        }, products), "100");

        return Task.CompletedTask;
    }

    private static Task TestMacroSelectionAsync()
    {
        var domain = new RecipeDomainService();
        var result = domain.ApplyDishMacro("Томатный !суп !десерт", null);
        AssertEqual("Томатный !десерт", result.Name, "macro name");
        AssertEqual("Суп", result.Category, "macro category");

        var explicitResult = domain.ApplyDishMacro("ягодный !десерт", "Напиток");
        AssertEqual("ягодный", explicitResult.Name, "явное имя");
        AssertEqual("Напиток", explicitResult.Category, "явная категория");
        return Task.CompletedTask;
    }

    private static Task TestDishNormalizationAsync()
    {
        var domain = new RecipeDomainService();
        var products = CreateProducts(domain);

        var dish = domain.NormalizeDish(new DishRequest
        {
            Name = "!салат Овощной",
            Photos = [],
            PortionSize = 250,
            Flags = ["Веган", "Без глютена", "Без сахара"],
            Items =
            [
                new DishItemRequest { ProductId = products[0].Id, Quantity = 150 },
                new DishItemRequest { ProductId = products[1].Id, Quantity = 100 }
            ]
        }, products);

        AssertEqual("Овощной", dish.Name, "название блюда");
        AssertEqual("Салат", dish.Category, "категория блюда");
        AssertSequence(["Веган", "Без глютена"], dish.Flags, "флаги блюда");
        AssertSequence(["Веган", "Без глютена"], dish.AvailableFlags, "доступные флаги блюда");
        AssertEqual(150m, dish.NutritionDraft.Calories, "калории");
        AssertEqual(13.5m, dish.NutritionDraft.Proteins, "белки");
        AssertEqual(7.3m, dish.NutritionDraft.Fats, "жиры");
        AssertEqual(6.5m, dish.NutritionDraft.Carbs, "углеводы");
        return Task.CompletedTask;
    }

    private static Task TestProductFilteringAsync()
    {
        var domain = new RecipeDomainService();
        var products = new List<Product>
        {
            domain.NormalizeProduct(new ProductRequest
            {
                Name = "Брокколи",
                Photos = [],
                Calories = 34,
                Proteins = 2.8m,
                Fats = 0.4m,
                Carbs = 6.6m,
                Category = "Овощи",
                CookingState = "Готовый к употреблению",
                Flags = ["Веган", "Без глютена", "Без сахара"]
            }),
            domain.NormalizeProduct(new ProductRequest
            {
                Name = "Шоколад",
                Photos = [],
                Calories = 500,
                Proteins = 5,
                Fats = 30,
                Carbs = 50,
                Category = "Сладости",
                CookingState = "Готовый к употреблению",
                Flags = ["Без глютена"]
            })
        };

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["search"] = "брок",
            ["category"] = "Овощи",
            ["flags"] = "Веган",
            ["sort"] = "name"
        });

        var result = domain.FilterProducts(products, query);
        AssertEqual(1, result.Count, "количество продуктов, попавших под фильтр");
        AssertEqual("Брокколи", result[0].Name, "имя продукта, попавшее под фильтр");
        return Task.CompletedTask;
    }

    private static Task TestDishFilteringAsync()
    {
        var domain = new RecipeDomainService();
        var products = CreateProducts(domain);
        var dishes = new List<Dish>
        {
            domain.NormalizeDish(new DishRequest
            {
                Name = "Овощной суп",
                Photos = [],
                PortionSize = 300,
                Category = "Суп",
                Flags = ["Веган", "Без глютена"],
                Items =
                [
                    new DishItemRequest { ProductId = products[0].Id, Quantity = 100 },
                    new DishItemRequest { ProductId = products[1].Id, Quantity = 100 }
                ]
            }, products),
            domain.NormalizeDish(new DishRequest
            {
                Name = "Тофу перекус",
                Photos = [],
                PortionSize = 150,
                Category = "Перекус",
                Flags = ["Веган", "Без глютена"],
                Items = [new DishItemRequest { ProductId = products[1].Id, Quantity = 100 }]
            }, products)
        };

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["search"] = "суп",
            ["category"] = "Суп",
            ["flags"] = "Веган"
        });

        var result = domain.FilterDishes(dishes, products, query);
        AssertEqual(1, result.Count, "количество блюд попавших под фильтр");
        AssertEqual("Овощной суп", result[0].Name, "имя блюда попавшего под фильтр");
        return Task.CompletedTask;
    }

    private static async Task TestDeleteConflictApiAsync()
    {
        var database = new DatabaseModel
        {
            Products =
            [
                new Product
                {
                    Id = "p1",
                    Name = "рис",
                    Photos = [],
                    Calories = 330,
                    Proteins = 7,
                    Fats = 1,
                    Carbs = 74,
                    Category = "крупы",
                    CookingState = "требует приготовления",
                    Flags = ["веган", "без глютена", "без сахара"],
                    CreatedAt = DateTime.UtcNow
                }
            ],
            Dishes =
            [
                new Dish
                {
                    Id = "d1",
                    Name = "рисовая каша",
                    Photos = [],
                    Calories = 165,
                    Proteins = 3.5m,
                    Fats = 0.5m,
                    Carbs = 37,
                    PortionSize = 150,
                    Category = "перекус",
                    Flags = ["веган", "без глютена", "без сахара"],
                    AvailableFlags = ["веган", "без глютена", "без сахара"],
                    NutritionDraft = new NutritionDraft
                    {
                        Calories = 165,
                        Proteins = 3.5m,
                        Fats = 0.5m,
                        Carbs = 37
                    },
                    CreatedAt = DateTime.UtcNow,
                    Items = [new DishItem { ProductId = "p1", Quantity = 50 }]
                }
            ]
        };

        await using var host = await StartAppAsync(database);
        var response = await host.Client.DeleteAsync($"{host.BaseUrl}/api/products/p1");
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        AssertEqual(HttpStatusCode.Conflict, response.StatusCode, "delete status");
        AssertEqual("Нельзя удалить продукт, который используется в блюдах.", payload.GetProperty("error").GetString(), "delete error");
        AssertEqual("рисовая каша", payload.GetProperty("dishes")[0].GetProperty("name").GetString(), "delete conflict");
    }

    private static async Task TestDishCreationApiAsync()
    {
        var database = new DatabaseModel
        {
            Products =
            [
                new Product
                {
                    Id = "p1",
                    Name = "Томат",
                    Photos = [],
                    Calories = 20,
                    Proteins = 1,
                    Fats = 0.2m,
                    Carbs = 3,
                    Category = "Овощи",
                    CookingState = "Готовый к употреблению",
                    Flags = ["Веган", "Без глютена", "Без сахара"],
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };

        await using var host = await StartAppAsync(database);
        var response = await host.Client.PostAsJsonAsync($"{host.BaseUrl}/api/dishes", new DishRequest
        {
            Name = "!суп Томатный",
            Photos = [],
            PortionSize = 300,
            Flags = ["Веган", "Без глютена", "Без сахара"],
            Items = [new DishItemRequest { ProductId = "p1", Quantity = 200 }]
        }, JsonOptions);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        AssertEqual(HttpStatusCode.Created, response.StatusCode, "Create status");
        AssertEqual("Томатный", payload.GetProperty("name").GetString(), "Dish name");
        AssertEqual("Суп", payload.GetProperty("category").GetString(), "Dish category");
        AssertEqual(40m, payload.GetProperty("nutritionDraft").GetProperty("calories").GetDecimal(), "Draft calories");
    }

    private static List<Product> CreateProducts(RecipeDomainService domain)
    {
        return
        [
            domain.NormalizeProduct(new ProductRequest
            {
                Name = "Огурец",
                Photos = [],
                Calories = 20,
                Proteins = 1,
                Fats = 0.2m,
                Carbs = 3,
                Composition = "Огурец",
                Category = "Овощи",
                CookingState = "Готовый к употреблению",
                Flags = ["Веган", "Без глютена", "Без сахара"]
            }),
            domain.NormalizeProduct(new ProductRequest
            {
                Name = "Тофу",
                Photos = [],
                Calories = 120,
                Proteins = 12,
                Fats = 7,
                Carbs = 2,
                Composition = "Тофу",
                Category = "Консервы",
                CookingState = "Готовый к употреблению",
                Flags = ["Веган", "Без глютена"]
            })
        ];
    }

    private static async Task<TestHost> StartAppAsync(DatabaseModel seed)
    {
        var root = Path.Combine(Path.GetTempPath(), $"recipe-book-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var dataPath = Path.Combine(root, "db.json");
        await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(seed, JsonOptions));

        var staticPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "public"));
        var app = RecipeBookApp.Build([], dataPath, staticPath);
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        var address = addresses.Addresses.Single();
        return new TestHost(app, new HttpClient(), address);
    }

    private static void AssertThrows<TException>(Action action, string expectedMessagePart) where TException : Exception
    {
        try
        {
            action();
            throw new InvalidOperationException("Expected exception was not thrown.");
        }
        catch (TException exception)
        {
            if (!exception.Message.Contains(expectedMessagePart, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unexpected exception message: {exception.Message}");
            }
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected '{expected}', actual '{actual}'.");
        }
    }

    private static void AssertSequence<T>(IReadOnlyCollection<T> expected, IReadOnlyCollection<T> actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException($"{message}: expected '{string.Join(", ", expected)}', actual '{string.Join(", ", actual)}'.");
        }
    }

    private sealed class TestHost(WebApplication app, HttpClient client, string baseUrl) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public string BaseUrl { get; } = baseUrl;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Api.Domain;

namespace RecipeBook.IntegrationTests;

internal sealed class ApiTestHost : IAsyncDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly WebApplication _app;
    private readonly string _workDirectory;

    private ApiTestHost(WebApplication app, HttpClient client, string workDirectory)
    {
        _app = app;
        Client = client;
        _workDirectory = workDirectory;
    }

    public HttpClient Client { get; }

    public string DatabasePath => Path.Combine(_workDirectory, "db.json");

    /// <summary>
    /// Глобальная настройка API-тестов: запускает настоящий конвейер ASP.NET Core
    /// поверх файлового хранилища. Тесты не имитируют маршрутизацию, сериализацию,
    /// сервисы приложения или сохранение данных.
    /// </summary>
    public static async Task<ApiTestHost> StartAsync(DatabaseModel? seed = null)
    {
        var workDirectory = Path.Combine(Path.GetTempPath(), $"recipe-book-api-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDirectory);
        var databasePath = Path.Combine(workDirectory, "db.json");
        await File.WriteAllTextAsync(databasePath, JsonSerializer.Serialize(seed ?? new DatabaseModel(), JsonOptions));

        var staticPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "public"));
        var app = RecipeBookApp.Build([], databasePath, staticPath);
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var baseAddress = addresses?.Addresses.Single() ?? throw new InvalidOperationException("The test server did not publish an address.");
        var client = new HttpClient { BaseAddress = new Uri(baseAddress) };

        return new ApiTestHost(app, client, workDirectory);
    }

    public async Task<DatabaseModel> ReadDatabaseAsync()
    {
        var database = await JsonSerializer.DeserializeAsync<DatabaseModel>(
            File.OpenRead(DatabasePath),
            JsonOptions);

        return database ?? new DatabaseModel();
    }

    public async Task<HttpResponseMessage> CreateProductAsync(ProductRequest request) =>
        await Client.PostAsJsonAsync("/api/products", request, JsonOptions);

    public async Task<HttpResponseMessage> CreateDishAsync(DishRequest request) =>
        await Client.PostAsJsonAsync("/api/dishes", request, JsonOptions);

    /// <summary>
    /// Завершение работы тестового хоста: останавливает Kestrel, освобождает HttpClient
    /// и удаляет временную файловую базу данных текущего теста.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();

        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }
}

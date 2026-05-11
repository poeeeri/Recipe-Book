using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using RecipeBook.Api.Domain;

namespace RecipeBook.SystemTests;

internal sealed class SystemTestHost : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly WebApplication? _app;
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly string? _workDirectory;

    private SystemTestHost(WebApplication? app, IPlaywright playwright, IBrowser browser, string? workDirectory, string baseUrl)
    {
        _app = app;
        _playwright = playwright;
        _browser = browser;
        _workDirectory = workDirectory;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    /// <summary>
    /// Глобальная настройка системных UI-тестов. По умолчанию запускает настоящее
    /// приложение с временной файловой БД. Если задан RECIPE_BOOK_UI_BASE_URL,
    /// открывает уже запущенное приложение, чтобы можно было показать запросы
    /// в отдельном терминале backend-а.
    /// </summary>
    public static async Task<SystemTestHost> StartAsync(DatabaseModel? seed = null)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var externalBaseUrl = Environment.GetEnvironmentVariable("RECIPE_BOOK_UI_BASE_URL");
        if (!string.IsNullOrWhiteSpace(externalBaseUrl))
        {
            return new SystemTestHost(
                app: null,
                playwright,
                browser,
                workDirectory: null,
                baseUrl: externalBaseUrl);
        }

        var workDirectory = Path.Combine(Path.GetTempPath(), $"recipe-book-ui-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDirectory);
        var databasePath = Path.Combine(workDirectory, "db.json");
        await File.WriteAllTextAsync(databasePath, JsonSerializer.Serialize(seed ?? new DatabaseModel(), JsonOptions));

        var staticPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "public"));
        var app = RecipeBookApp.Build([], databasePath, staticPath);
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var baseUrl = addresses?.Addresses.Single() ?? throw new InvalidOperationException("The system test server did not publish an address.");

        return new SystemTestHost(app, playwright, browser, workDirectory, baseUrl);
    }

    public async Task<IPage> NewPageAsync()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    /// <summary>
    /// Завершение системного теста: закрывает браузер. Локально поднятый backend
    /// останавливается и его временная БД удаляется; внешний backend не трогается.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        if (_workDirectory is not null && Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }
}

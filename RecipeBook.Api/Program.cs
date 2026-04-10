using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RecipeBook.Api.Application;
using RecipeBook.Api.Data;
using RecipeBook.Api.Domain;
using RecipeBook.Api.Infrastructure;

var app = RecipeBookApp.Build(args);
app.Run();

public partial class Program;

public static class RecipeBookApp
{
    public static WebApplication Build(string[] args, string? databasePath = null, string? staticFilesPath = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:3000");

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        if (!string.IsNullOrWhiteSpace(databasePath))
        {
            builder.Services.AddSingleton<IRecipeStore>(new FileDatabaseStore(databasePath));
        }
        else
        {
            var provider = builder.Configuration["Storage:Provider"] ?? "Postgres";
            if (string.Equals(provider, "File", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = builder.Configuration["Storage:FilePath"] ??
                    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "data", "db.json"));
                builder.Services.AddSingleton<IRecipeStore>(new FileDatabaseStore(filePath));
            }
            else
            {
                var connectionString = builder.Configuration.GetConnectionString("RecipeBook")
                    ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:RecipeBook.");

                builder.Services.AddDbContext<RecipeBookDbContext>(options =>
                    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName)));
                builder.Services.AddScoped<IRecipeStore, EfRecipeStore>();
            }
        }

        builder.Services.AddSingleton<RecipeDomainService>();

        var app = builder.Build();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                if (feature?.Error is RecipeValidationException validationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = validationException.Message });
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера." });
            });
        });

        var publicPath = staticFilesPath ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "public"));
        if (Directory.Exists(publicPath))
        {
            var provider = new PhysicalFileProvider(publicPath);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
        }

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RecipeBookDbContext>();
            dbContext.Database.Migrate();
        }

        app.MapRecipeApi();
        return app;
    }
}

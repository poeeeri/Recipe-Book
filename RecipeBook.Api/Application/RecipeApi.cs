using RecipeBook.Api.Domain;
using RecipeBook.Api.Infrastructure;

namespace RecipeBook.Api.Application;

public static class RecipeApi
{
    public static void MapRecipeApi(this WebApplication app)
    {
        app.MapGet("/api/meta", () => Results.Ok(new MetaResponse()));

        app.MapGet("/api/products", async (HttpContext context, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            return Results.Ok(domain.FilterProducts(database.Products, context.Request.Query));
        });

        app.MapPost("/api/products", async (ProductRequest request, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var product = domain.NormalizeProduct(request);
            database.Products.Add(product);
            await store.WriteAsync(database, cancellationToken);
            return Results.Created($"/api/products/{product.Id}", product);
        }).AddEndpointFilter(HandleValidationExceptionAsync);

        app.MapGet("/api/products/{id}", async (string id, IRecipeStore store, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var product = database.Products.FirstOrDefault(item => item.Id == id);
            return product is null ? Results.NotFound(new { error = "Ресурс не найден." }) : Results.Ok(product);
        });

        app.MapPut("/api/products/{id}", async (string id, ProductRequest request, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var index = database.Products.FindIndex(item => item.Id == id);
            if (index < 0)
            {
                return Results.NotFound(new { error = "Ресурс не найден." });
            }

            var updatedProduct = domain.NormalizeProduct(request, database.Products[index]);
            database.Products[index] = updatedProduct;
            database.Dishes = database.Dishes.Select(dish => domain.NormalizeDish(ToRequest(dish), database.Products, dish)).ToList();
            await store.WriteAsync(database, cancellationToken);
            return Results.Ok(updatedProduct);
        }).AddEndpointFilter(HandleValidationExceptionAsync);

        app.MapDelete("/api/products/{id}", async (string id, IRecipeStore store, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var product = database.Products.FirstOrDefault(item => item.Id == id);
            if (product is null)
            {
                return Results.NotFound(new { error = "Ресурс не найден." });
            }

            var usedIn = database.Dishes
                .Where(dish => dish.Items.Any(item => item.ProductId == id))
                .Select(dish => new ProductUsageInfo { Id = dish.Id, Name = dish.Name })
                .ToList();

            if (usedIn.Count > 0)
            {
                return Results.Json(new
                {
                    error = "Нельзя удалить продукт, который используется в блюдах.",
                    dishes = usedIn
                }, statusCode: StatusCodes.Status409Conflict);
            }

            database.Products.Remove(product);
            await store.WriteAsync(database, cancellationToken);
            return Results.Ok(new { ok = true });
        });

        app.MapGet("/api/dishes", async (HttpContext context, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            return Results.Ok(domain.FilterDishes(database.Dishes, database.Products, context.Request.Query));
        });

        app.MapPost("/api/dishes", async (DishRequest request, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var dish = domain.NormalizeDish(request, database.Products);
            database.Dishes.Add(dish);
            await store.WriteAsync(database, cancellationToken);
            return Results.Created($"/api/dishes/{dish.Id}", domain.PresentDish(dish, database.Products));
        }).AddEndpointFilter(HandleValidationExceptionAsync);

        app.MapGet("/api/dishes/{id}", async (string id, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var dish = database.Dishes.FirstOrDefault(item => item.Id == id);
            return dish is null
                ? Results.NotFound(new { error = "Ресурс не найден." })
                : Results.Ok(domain.PresentDish(dish, database.Products));
        });

        app.MapPut("/api/dishes/{id}", async (string id, DishRequest request, IRecipeStore store, RecipeDomainService domain, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var index = database.Dishes.FindIndex(item => item.Id == id);
            if (index < 0)
            {
                return Results.NotFound(new { error = "Ресурс не найден." });
            }

            var updatedDish = domain.NormalizeDish(request, database.Products, database.Dishes[index]);
            database.Dishes[index] = updatedDish;
            await store.WriteAsync(database, cancellationToken);
            return Results.Ok(domain.PresentDish(updatedDish, database.Products));
        }).AddEndpointFilter(HandleValidationExceptionAsync);

        app.MapDelete("/api/dishes/{id}", async (string id, IRecipeStore store, CancellationToken cancellationToken) =>
        {
            var database = await store.ReadAsync(cancellationToken);
            var dish = database.Dishes.FirstOrDefault(item => item.Id == id);
            if (dish is null)
            {
                return Results.NotFound(new { error = "Ресурс не найден." });
            }

            database.Dishes.Remove(dish);
            await store.WriteAsync(database, cancellationToken);
            return Results.Ok(new { ok = true });
        });
    }

    private static DishRequest ToRequest(Dish dish) =>
        new()
        {
            Name = dish.Name,
            Photos = [.. dish.Photos],
            Calories = dish.Calories,
            Proteins = dish.Proteins,
            Fats = dish.Fats,
            Carbs = dish.Carbs,
            PortionSize = dish.PortionSize,
            Category = dish.Category,
            Flags = [.. dish.Flags],
            Items = [.. dish.Items.Select(item => new DishItemRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })]
        };

    private static async ValueTask<object?> HandleValidationExceptionAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (RecipeValidationException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }
}

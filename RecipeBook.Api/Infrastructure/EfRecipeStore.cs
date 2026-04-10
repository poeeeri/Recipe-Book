using Microsoft.EntityFrameworkCore;
using RecipeBook.Api.Data;
using RecipeBook.Api.Domain;

namespace RecipeBook.Api.Infrastructure;

public sealed class EfRecipeStore(RecipeBookDbContext dbContext) : IRecipeStore
{
    public async Task<DatabaseModel> ReadAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Photos)
            .Include(x => x.Flags)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var dishes = await dbContext.Dishes
            .AsNoTracking()
            .Include(x => x.Photos)
            .Include(x => x.Flags)
            .Include(x => x.AvailableFlags)
            .Include(x => x.Items)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new DatabaseModel
        {
            Products = [.. products.Select(MapProduct)],
            Dishes = [.. dishes.Select(MapDish)]
        };
    }

    public async Task WriteAsync(DatabaseModel database, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.DishItems.RemoveRange(dbContext.DishItems);
        dbContext.DishAvailableFlags.RemoveRange(dbContext.DishAvailableFlags);
        dbContext.DishFlags.RemoveRange(dbContext.DishFlags);
        dbContext.DishPhotos.RemoveRange(dbContext.DishPhotos);
        dbContext.Dishes.RemoveRange(dbContext.Dishes);
        dbContext.ProductFlags.RemoveRange(dbContext.ProductFlags);
        dbContext.ProductPhotos.RemoveRange(dbContext.ProductPhotos);
        dbContext.Products.RemoveRange(dbContext.Products);
        await dbContext.SaveChangesAsync(cancellationToken);

        var productEntities = database.Products.Select(MapProductEntity).ToList();
        await dbContext.Products.AddRangeAsync(productEntities, cancellationToken);

        var dishEntities = database.Dishes.Select(MapDishEntity).ToList();
        await dbContext.Dishes.AddRangeAsync(dishEntities, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static Product MapProduct(ProductEntity entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Photos = [.. entity.Photos.OrderBy(x => x.SortOrder).Select(x => x.Url)],
            Calories = entity.Calories,
            Proteins = entity.Proteins,
            Fats = entity.Fats,
            Carbs = entity.Carbs,
            Composition = entity.Composition,
            Category = entity.Category,
            CookingState = entity.CookingState,
            Flags = [.. entity.Flags.OrderBy(x => x.Value).Select(x => x.Value)],
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    private static Dish MapDish(DishEntity entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Photos = [.. entity.Photos.OrderBy(x => x.SortOrder).Select(x => x.Url)],
            Calories = entity.Calories,
            Proteins = entity.Proteins,
            Fats = entity.Fats,
            Carbs = entity.Carbs,
            PortionSize = entity.PortionSize,
            Category = entity.Category,
            Flags = [.. entity.Flags.OrderBy(x => x.Value).Select(x => x.Value)],
            AvailableFlags = [.. entity.AvailableFlags.OrderBy(x => x.Value).Select(x => x.Value)],
            NutritionDraft = new NutritionDraft
            {
                Calories = entity.DraftCalories,
                Proteins = entity.DraftProteins,
                Fats = entity.DraftFats,
                Carbs = entity.DraftCarbs
            },
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Items = [.. entity.Items.OrderBy(x => x.Id).Select(x => new DishItem
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity
            })]
        };

    private static ProductEntity MapProductEntity(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Calories = product.Calories,
            Proteins = product.Proteins,
            Fats = product.Fats,
            Carbs = product.Carbs,
            Composition = product.Composition,
            Category = product.Category,
            CookingState = product.CookingState,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Photos = [.. product.Photos.Select((photo, index) => new ProductPhotoEntity
            {
                SortOrder = index,
                Url = photo,
                ProductId = product.Id
            })],
            Flags = [.. product.Flags.Select(flag => new ProductFlagEntity
            {
                Value = flag,
                ProductId = product.Id
            })]
        };

    private static DishEntity MapDishEntity(Dish dish) =>
        new()
        {
            Id = dish.Id,
            Name = dish.Name,
            Calories = dish.Calories,
            Proteins = dish.Proteins,
            Fats = dish.Fats,
            Carbs = dish.Carbs,
            PortionSize = dish.PortionSize,
            Category = dish.Category,
            DraftCalories = dish.NutritionDraft.Calories,
            DraftProteins = dish.NutritionDraft.Proteins,
            DraftFats = dish.NutritionDraft.Fats,
            DraftCarbs = dish.NutritionDraft.Carbs,
            CreatedAt = dish.CreatedAt,
            UpdatedAt = dish.UpdatedAt,
            Photos = [.. dish.Photos.Select((photo, index) => new DishPhotoEntity
            {
                SortOrder = index,
                Url = photo,
                DishId = dish.Id
            })],
            Flags = [.. dish.Flags.Select(flag => new DishFlagEntity
            {
                Value = flag,
                DishId = dish.Id
            })],
            AvailableFlags = [.. dish.AvailableFlags.Select(flag => new DishAvailableFlagEntity
            {
                Value = flag,
                DishId = dish.Id
            })],
            Items = [.. dish.Items.Select(item => new DishItemEntity
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                DishId = dish.Id
            })]
        };
}

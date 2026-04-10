using RecipeBook.Api.Domain;

namespace RecipeBook.Api.Infrastructure;

public interface IRecipeStore
{
    Task<DatabaseModel> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(DatabaseModel database, CancellationToken cancellationToken = default);
}

using System.Text.Json;
using RecipeBook.Api.Domain;

namespace RecipeBook.Api.Infrastructure;

public sealed class FileDatabaseStore : IRecipeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _databasePath;

    public FileDatabaseStore(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<DatabaseModel> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureFileExistsAsync(cancellationToken);
            await using var stream = File.OpenRead(_databasePath);
            var database = await JsonSerializer.DeserializeAsync<DatabaseModel>(stream, JsonOptions, cancellationToken);
            return database ?? new DatabaseModel();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteAsync(DatabaseModel database, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureFileExistsAsync(cancellationToken);
            await using var stream = File.Create(_databasePath);
            await JsonSerializer.SerializeAsync(stream, database, JsonOptions, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureFileExistsAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_databasePath))
        {
            await File.WriteAllTextAsync(_databasePath, JsonSerializer.Serialize(new DatabaseModel(), JsonOptions), cancellationToken);
        }
    }
}

namespace RecipeBook.Api.Domain;

public sealed class RecipeValidationException(string message) : Exception(message);

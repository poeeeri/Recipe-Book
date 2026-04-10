using System.Globalization;
using System.Text.RegularExpressions;

namespace RecipeBook.Api.Domain;

public sealed class RecipeDomainService
{
    public Product NormalizeProduct(ProductRequest request, Product? existingProduct = null)
    {
        var product = new Product
        {
            Id = existingProduct?.Id ?? Guid.NewGuid().ToString("N"),
            Name = ValidateName("Название", request.Name),
            Photos = NormalizePhotos(request.Photos),
            Calories = ValidateNumber("Калорийность", request.Calories, min: 0),
            Proteins = ValidateNumber("Белки", request.Proteins, min: 0, max: 100),
            Fats = ValidateNumber("Жиры", request.Fats, min: 0, max: 100),
            Carbs = ValidateNumber("Углеводы", request.Carbs, min: 0, max: 100),
            Composition = NormalizeOptionalText(request.Composition),
            Category = ValidateEnum("Категория", request.Category, RecipeConstants.ProductCategories),
            CookingState = ValidateEnum("Необходимость готовки", request.CookingState, RecipeConstants.ProductCookingStates),
            Flags = ValidateFlags(request.Flags),
            CreatedAt = existingProduct?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = existingProduct is null ? null : DateTime.UtcNow
        };

        ValidateBjuPer100g(product.Proteins, product.Fats, product.Carbs);
        return product;
    }

    public Dish NormalizeDish(DishRequest request, IReadOnlyCollection<Product> products, Dish? existingDish = null)
    {
        var productById = products.ToDictionary(product => product.Id, StringComparer.Ordinal);
        var macroResult = ApplyDishMacro(request.Name, request.Category);
        var items = NormalizeDishItems(request.Items, productById);
        var draft = CalculateDishNutrition(items, productById);
        var availableFlags = GetAvailableDishFlags(items, productById);
        var selectedFlags = ValidateFlags(request.Flags).Where(availableFlags.Contains).ToList();
        var portionSize = ValidateNumber("Размер порции", request.PortionSize, greaterThan: 0);

        var dish = new Dish
        {
            Id = existingDish?.Id ?? Guid.NewGuid().ToString("N"),
            Name = ValidateName("Название", macroResult.Name),
            Photos = NormalizePhotos(request.Photos),
            Calories = ValidateNumber("Калорийность", request.Calories ?? draft.Calories, min: 0),
            Proteins = ValidateNumber("Белки", request.Proteins ?? draft.Proteins, min: 0),
            Fats = ValidateNumber("Жиры", request.Fats ?? draft.Fats, min: 0),
            Carbs = ValidateNumber("Углеводы", request.Carbs ?? draft.Carbs, min: 0),
            PortionSize = portionSize,
            Category = ValidateEnum("Категория", macroResult.Category, RecipeConstants.DishCategories),
            Flags = selectedFlags,
            AvailableFlags = availableFlags,
            Items = items,
            NutritionDraft = draft,
            CreatedAt = existingDish?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = existingDish is null ? null : DateTime.UtcNow
        };

        ValidateDishBjuPer100g(dish.Proteins, dish.Fats, dish.Carbs, dish.PortionSize);
        return dish;
    }

    public MacroResult ApplyDishMacro(string? rawName, string? explicitCategory)
    {
        var name = (rawName ?? string.Empty).Trim();
        var lowered = name.ToLowerInvariant();
        string? matchedMacro = null;
        string? detectedCategory = null;
        var matchedIndex = int.MaxValue;

        foreach (var pair in RecipeConstants.CategoryMacros)
        {
            var index = lowered.IndexOf(pair.Key, StringComparison.Ordinal);
            if (index >= 0 && index < matchedIndex)
            {
                matchedIndex = index;
                matchedMacro = pair.Key;
                detectedCategory = pair.Value;
            }
        }

        if (matchedMacro is not null)
        {
            name = string.Concat(name.AsSpan(0, matchedIndex), " ", name.AsSpan(matchedIndex + matchedMacro.Length));
            name = Regex.Replace(name, "\\s+", " ").Trim();
        }

        return new MacroResult
        {
            Name = name,
            Category = string.IsNullOrWhiteSpace(explicitCategory) ? detectedCategory : explicitCategory
        };
    }

    public NutritionDraft CalculateDishNutrition(IEnumerable<DishItem> items, IReadOnlyDictionary<string, Product> productsById)
    {
        decimal calories = 0;
        decimal proteins = 0;
        decimal fats = 0;
        decimal carbs = 0;

        foreach (var item in items)
        {
            if (!productsById.TryGetValue(item.ProductId, out var product))
            {
                throw new RecipeValidationException($"Продукт \"{item.ProductId}\" не найден.");
            }

            var ratio = item.Quantity / 100m;
            calories += product.Calories * ratio;
            proteins += product.Proteins * ratio;
            fats += product.Fats * ratio;
            carbs += product.Carbs * ratio;
        }

        return new NutritionDraft
        {
            Calories = Round(calories),
            Proteins = Round(proteins),
            Fats = Round(fats),
            Carbs = Round(carbs)
        };
    }

    public List<string> GetAvailableDishFlags(IEnumerable<DishItem> items, IReadOnlyDictionary<string, Product> productsById)
    {
        var normalizedItems = items.ToList();
        if (normalizedItems.Count == 0)
        {
            return [];
        }

        return RecipeConstants.Flags
            .Where(flag => normalizedItems.All(item =>
                productsById.TryGetValue(item.ProductId, out var product) &&
                product.Flags.Contains(flag, StringComparer.Ordinal)))
            .ToList();
    }

    public DishDetails PresentDish(Dish dish, IReadOnlyCollection<Product> products)
    {
        var productById = products.ToDictionary(product => product.Id, StringComparer.Ordinal);
        return new DishDetails
        {
            Id = dish.Id,
            Name = dish.Name,
            Photos = [.. dish.Photos],
            Calories = dish.Calories,
            Proteins = dish.Proteins,
            Fats = dish.Fats,
            Carbs = dish.Carbs,
            PortionSize = dish.PortionSize,
            Category = dish.Category,
            Flags = [.. dish.Flags],
            AvailableFlags = [.. dish.AvailableFlags],
            NutritionDraft = new NutritionDraft
            {
                Calories = dish.NutritionDraft.Calories,
                Proteins = dish.NutritionDraft.Proteins,
                Fats = dish.NutritionDraft.Fats,
                Carbs = dish.NutritionDraft.Carbs
            },
            CreatedAt = dish.CreatedAt,
            UpdatedAt = dish.UpdatedAt,
            Items = [.. dish.Items.Select(item => new DishItemDetails
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Product = CloneProduct(productById[item.ProductId])
            })]
        };
    }

    public List<Product> FilterProducts(IEnumerable<Product> products, IQueryCollection query)
    {
        var result = products.ToList();
        var search = query["search"].ToString().Trim();
        var category = query["category"].ToString().Trim();
        var cookingState = query["cookingState"].ToString().Trim();
        var sort = query["sort"].ToString().Trim();
        var order = string.Equals(query["order"], "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        var flags = query["flags"].Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToList();

        if (!string.IsNullOrWhiteSpace(category))
        {
            result = result.Where(product => product.Category == category).ToList();
        }

        if (!string.IsNullOrWhiteSpace(cookingState))
        {
            result = result.Where(product => product.CookingState == cookingState).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            result = result.Where(product => product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (flags.Count > 0)
        {
            result = result.Where(product => flags.All(flag => product.Flags.Contains(flag, StringComparer.Ordinal))).ToList();
        }

        result = (sort, order) switch
        {
            ("name", "desc") => [.. result.OrderByDescending(product => product.Name, StringComparer.Ordinal)],
            ("name", _) => [.. result.OrderBy(product => product.Name, StringComparer.Ordinal)],
            ("calories", "desc") => [.. result.OrderByDescending(product => product.Calories)],
            ("calories", _) => [.. result.OrderBy(product => product.Calories)],
            ("proteins", "desc") => [.. result.OrderByDescending(product => product.Proteins)],
            ("proteins", _) => [.. result.OrderBy(product => product.Proteins)],
            ("fats", "desc") => [.. result.OrderByDescending(product => product.Fats)],
            ("fats", _) => [.. result.OrderBy(product => product.Fats)],
            ("carbs", "desc") => [.. result.OrderByDescending(product => product.Carbs)],
            ("carbs", _) => [.. result.OrderBy(product => product.Carbs)],
            _ => result
        };

        return result;
    }

    public List<DishDetails> FilterDishes(IEnumerable<Dish> dishes, IReadOnlyCollection<Product> products, IQueryCollection query)
    {
        var result = dishes.ToList();
        var search = query["search"].ToString().Trim();
        var category = query["category"].ToString().Trim();
        var flags = query["flags"].Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToList();

        if (!string.IsNullOrWhiteSpace(category))
        {
            result = result.Where(dish => dish.Category == category).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            result = result.Where(dish => dish.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (flags.Count > 0)
        {
            result = result.Where(dish => flags.All(flag => dish.Flags.Contains(flag, StringComparer.Ordinal))).ToList();
        }

        return [.. result.Select(dish => PresentDish(dish, products))];
    }

    private static Product CloneProduct(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Photos = [.. product.Photos],
            Calories = product.Calories,
            Proteins = product.Proteins,
            Fats = product.Fats,
            Carbs = product.Carbs,
            Composition = product.Composition,
            Category = product.Category,
            CookingState = product.CookingState,
            Flags = [.. product.Flags],
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

    private static List<DishItem> NormalizeDishItems(IEnumerable<DishItemRequest>? items, IReadOnlyDictionary<string, Product> productsById)
    {
        var normalizedItems = (items ?? [])
            .Select(item => new DishItem
            {
                ProductId = (item.ProductId ?? string.Empty).Trim(),
                Quantity = ValidateNumber("Количество продукта", item.Quantity, greaterThan: 0)
            })
            .ToList();

        if (normalizedItems.Count == 0)
        {
            throw new RecipeValidationException("Поле \"Состав\" должно содержать минимум один продукт.");
        }

        foreach (var item in normalizedItems)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId))
            {
                throw new RecipeValidationException("Для каждой записи в поле \"Состав\" требуется продукт.");
            }

            if (!productsById.ContainsKey(item.ProductId))
            {
                throw new RecipeValidationException($"Продукт \"{item.ProductId}\" не найден.");
            }
        }

        return normalizedItems;
    }

    private static decimal ValidateNumber(string fieldName, decimal? value, decimal? min = null, decimal? max = null, decimal? greaterThan = null)
    {
        if (value is null)
        {
            throw new RecipeValidationException($"Поле \"{fieldName}\" обязательно.");
        }

        var normalized = Round(value.Value);

        if (min.HasValue && normalized < min.Value)
        {
            throw new RecipeValidationException($"Поле \"{fieldName}\" должно быть не меньше {Format(min.Value)}.");
        }

        if (max.HasValue && normalized > max.Value)
        {
            throw new RecipeValidationException($"Поле \"{fieldName}\" должно быть не больше {Format(max.Value)}.");
        }

        if (greaterThan.HasValue && normalized <= greaterThan.Value)
        {
            throw new RecipeValidationException($"Поле \"{fieldName}\" должно быть больше {Format(greaterThan.Value)}.");
        }

        return normalized;
    }

    private static string ValidateName(string fieldName, string? value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length < 2)
        {
            throw new RecipeValidationException($"Поле \"{fieldName}\" должно содержать минимум 2 символа.");
        }

        return text;
    }

    private static string ValidateEnum(string fieldName, string? value, IEnumerable<string> allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value) || !allowedValues.Contains(value, StringComparer.Ordinal))
        {
            throw new RecipeValidationException($"Поле \"{fieldName}\" содержит недопустимое значение.");
        }

        return value;
    }

    private static List<string> NormalizePhotos(IEnumerable<string>? values)
    {
        var photos = (values ?? [])
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (photos.Count > 5)
        {
            throw new RecipeValidationException("Поле \"Фотографии\" может содержать не более 5 элементов.");
        }

        return photos;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static List<string> ValidateFlags(IEnumerable<string>? values)
    {
        var flags = (values ?? [])
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var flag in flags)
        {
            if (!RecipeConstants.Flags.Contains(flag, StringComparer.Ordinal))
            {
                throw new RecipeValidationException($"Флаг \"{flag}\" не поддерживается.");
            }
        }

        return flags;
    }

    private static void ValidateBjuPer100g(decimal proteins, decimal fats, decimal carbs)
    {
        if (proteins + fats + carbs > 100m)
        {
            throw new RecipeValidationException("Сумма БЖУ не может превышать 100.");
        }
    }

    private static void ValidateDishBjuPer100g(decimal proteinsPerPortion, decimal fatsPerPortion, decimal carbsPerPortion, decimal portionSize)
    {
        var proteinsPer100g = portionSize == 0 ? 0 : proteinsPerPortion / portionSize * 100m;
        var fatsPer100g = portionSize == 0 ? 0 : fatsPerPortion / portionSize * 100m;
        var carbsPer100g = portionSize == 0 ? 0 : carbsPerPortion / portionSize * 100m;
        ValidateBjuPer100g(Round(proteinsPer100g), Round(fatsPer100g), Round(carbsPer100g));
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Format(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

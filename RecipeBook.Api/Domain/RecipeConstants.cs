namespace RecipeBook.Api.Domain;

public static class RecipeConstants
{
    public static readonly string[] ProductCategories =
    [
        "Замороженный",
        "Мясной",
        "Овощи",
        "Зелень",
        "Специи",
        "Крупы",
        "Консервы",
        "Жидкость",
        "Сладости"
    ];

    public static readonly string[] ProductCookingStates =
    [
        "Готовый к употреблению",
        "Полуфабрикат",
        "Требует приготовления"
    ];

    public static readonly string[] DishCategories =
    [
        "Десерт",
        "Первое",
        "Второе",
        "Напиток",
        "Салат",
        "Суп",
        "Перекус"
    ];

    public static readonly string[] Flags =
    [
        "Веган",
        "Без глютена",
        "Без сахара"
    ];

    public static readonly IReadOnlyDictionary<string, string> CategoryMacros = new Dictionary<string, string>
    {
        ["!десерт"] = "Десерт",
        ["!первое"] = "Первое",
        ["!второе"] = "Второе",
        ["!напиток"] = "Напиток",
        ["!салат"] = "Салат",
        ["!суп"] = "Суп",
        ["!перекус"] = "Перекус"
    };
}

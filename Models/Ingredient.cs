namespace Dining.Models;

/// <summary>
/// Display data for one kind of ingredient (name + realistic colours).
/// Color is the fill, DarkColor is used for outlines and bin shadows.
/// </summary>
public sealed record Ingredient(IngredientType Type, string Name, string Color, string DarkColor)
{
    /// <summary>Every ingredient, in kitchen-grid order (left to right, top to bottom).</summary>
    public static readonly IReadOnlyList<Ingredient> All = new[]
    {
        new Ingredient(IngredientType.Bun,     "Buns",    "#F4B63F", "#B97D10"), // toasted golden yellow
        new Ingredient(IngredientType.Patty,   "Patties", "#7A4426", "#432312"), // grilled brown
        new Ingredient(IngredientType.Cheese,  "Cheese",  "#FF8A1F", "#B85A00"), // cheddar orange
        new Ingredient(IngredientType.Lettuce, "Lettuce", "#6CC24A", "#3A8A25"), // leafy green
        new Ingredient(IngredientType.Tomato,  "Tomato",  "#E63E32", "#9E1F17"), // ripe red
        new Ingredient(IngredientType.Onion,   "Onions",  "#C9A0DC", "#8250A0"), // red onion lavender
    };

    /// <summary>Everything that goes between the two buns.</summary>
    public static readonly IReadOnlyList<Ingredient> Fillings =
        All.Where(i => i.Type != IngredientType.Bun).ToList();

    public static Ingredient Get(IngredientType type) => All.First(i => i.Type == type);
}

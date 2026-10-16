namespace Dining.Models;

/// <summary>
/// A burger a customer wants. Layers are listed bottom to top,
/// so index 0 is the bottom bun and the last item is the top bun.
/// </summary>
public sealed class Order
{
    public IReadOnlyList<IngredientType> Layers { get; }

    public int Count => Layers.Count;

    public Order(IEnumerable<IngredientType> layers)
    {
        Layers = layers.ToList();
    }

    /// <summary>True when the tray holds exactly these layers in exactly this order.</summary>
    public bool Matches(IReadOnlyList<IngredientType> tray) =>
        tray.Count == Layers.Count && tray.SequenceEqual(Layers);

    /// <summary>Bun, 1 to 3 fillings, bun. Orders start simpler and get taller as the round goes on.</summary>
    public static Order CreateRandom(double elapsedSeconds, Random rng)
    {
        int maxFillings = elapsedSeconds < 15 ? 2 : 3;
        int fillingCount = rng.Next(1, maxFillings + 1);

        var layers = new List<IngredientType>(fillingCount + 2) { IngredientType.Bun };
        for (int i = 0; i < fillingCount; i++)
        {
            layers.Add(Ingredient.Fillings[rng.Next(Ingredient.Fillings.Count)].Type);
        }
        layers.Add(IngredientType.Bun);

        return new Order(layers);
    }

    /// <summary>The rare VIP order: bun, 8 to 11 fillings, bun.</summary>
    public static Order CreateMassive(Random rng)
    {
        int fillingCount = rng.Next(8, 12);

        var layers = new List<IngredientType>(fillingCount + 2) { IngredientType.Bun };
        for (int i = 0; i < fillingCount; i++)
        {
            layers.Add(Ingredient.Fillings[rng.Next(Ingredient.Fillings.Count)].Type);
        }
        layers.Add(IngredientType.Bun);

        return new Order(layers);
    }
}

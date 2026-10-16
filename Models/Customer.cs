namespace Dining.Models;

public enum CustomerState
{
    Waiting,
    Happy,
    Angry
}

public enum BodyShape
{
    Square,
    Triangle
}

/// <summary>One customer sitting at a table with an order and a patience timer.</summary>
public sealed class Customer
{
    private static int _nextId;

    public int Id { get; } = ++_nextId;
    public Order Order { get; }
    public BodyShape Shape { get; }
    public string BodyColor { get; }
    public string SkinColor { get; }

    public double PatienceMax { get; }
    public double Patience { get; set; }
    public CustomerState State { get; set; } = CustomerState.Waiting;

    /// <summary>How long a happy/angry customer stays on screen before leaving.</summary>
    public double LingerLeft { get; set; }

    /// <summary>1 = just sat down, 0 = out of patience.</summary>
    public double PatienceFraction => PatienceMax <= 0 ? 0 : Math.Clamp(Patience / PatienceMax, 0, 1);

    public Customer(Order order, BodyShape shape, string bodyColor, string skinColor, double patienceSeconds)
    {
        Order = order;
        Shape = shape;
        BodyColor = bodyColor;
        SkinColor = skinColor;
        PatienceMax = patienceSeconds;
        Patience = patienceSeconds;
    }
}

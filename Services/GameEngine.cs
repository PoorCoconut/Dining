using Dining.Models;

namespace Dining.Services;

/// <summary>
/// All RESTORUSH rules live here, away from the UI. The page calls Tick()
/// a few times per second and the components just read the public state.
/// </summary>
public sealed class GameEngine
{
    public const double RoundSeconds = 60;
    public const double TimeBonusSeconds = 1;
    public const int TableCount = 2;
    public const int MaxTrayLayers = 7;

    private const double LingerSeconds = 0.8;
    private const double PopupSeconds = 1.2;

    private static readonly string[] BodyColors =
        { "#E4572E", "#2F9C95", "#7B5EA7", "#6BA43A", "#EF6F9B", "#3B6EA5" };

    private static readonly string[] SkinColors =
        { "#FFE0BD", "#F1C27D", "#D9A066", "#A8713F", "#FDEBD0" };

    private readonly Random _rng = new();
    private readonly List<IngredientType> _tray = new();
    private readonly List<ScorePopup> _popups = new();
    private readonly Customer?[] _tables = new Customer?[TableCount];
    private readonly double[] _spawnDelay = new double[TableCount];

    private double _elapsed;
    private int _popupId;

    public GamePhase Phase { get; private set; } = GamePhase.Start;
    public double TimeLeft { get; private set; } = RoundSeconds;
    public int Score { get; private set; }
    public int ServedCount { get; private set; }
    public int HighScore { get; private set; }
    public bool IsNewBest { get; private set; }

    /// <summary>The burger being built, bottom to top.</summary>
    public IReadOnlyList<IngredientType> Tray => _tray;

    public IReadOnlyList<Customer?> Tables => _tables;
    public IReadOnlyList<ScorePopup> Popups => _popups;

    public void Start()
    {
        Phase = GamePhase.Playing;
        TimeLeft = RoundSeconds;
        Score = 0;
        ServedCount = 0;
        IsNewBest = false;
        _elapsed = 0;
        _tray.Clear();
        _popups.Clear();

        for (int i = 0; i < TableCount; i++)
        {
            _tables[i] = null;
            _spawnDelay[i] = 0.3 + i * 0.9;
        }
    }

    /// <summary>Advance the simulation by dt seconds.</summary>
    public void Tick(double dt)
    {
        if (Phase != GamePhase.Playing) return;

        dt = Math.Clamp(dt, 0, 0.25); // a backgrounded tab must not skip the whole round
        _elapsed += dt;
        TimeLeft -= dt;

        UpdateTables(dt);
        UpdatePopups(dt);

        if (TimeLeft <= 0)
        {
            TimeLeft = 0;
            EndRound();
        }
    }

    public void AddIngredient(IngredientType type)
    {
        if (Phase != GamePhase.Playing || _tray.Count >= MaxTrayLayers) return;
        _tray.Add(type);
    }

    public void UndoIngredient()
    {
        if (_tray.Count > 0) _tray.RemoveAt(_tray.Count - 1);
    }

    public void ClearTray() => _tray.Clear();

    /// <summary>Player clicked the customer at the given table.</summary>
    public ServeResult Serve(int table)
    {
        if (Phase != GamePhase.Playing || table < 0 || table >= TableCount) return ServeResult.Ignored;

        var customer = _tables[table];
        if (customer is null || customer.State != CustomerState.Waiting) return ServeResult.Ignored;

        if (_tray.Count == 0)
        {
            AddPopup(table, "Build a burger first!", null, false);
            return ServeResult.EmptyTray;
        }

        customer.LingerLeft = LingerSeconds;

        if (customer.Order.Matches(_tray))
        {
            int speedBonus = (int)Math.Round(customer.PatienceFraction * 10) * 10;
            int points = 100 + 50 * customer.Order.Count + speedBonus;

            Score += points;
            ServedCount++;
            TimeLeft += TimeBonusSeconds;
            customer.State = CustomerState.Happy;
            AddPopup(table, $"+{points}", $"+{(int)TimeBonusSeconds}s", true);

            _tray.Clear();
            return ServeResult.Correct;
        }

        customer.State = CustomerState.Angry;
        AddPopup(table, "Wrong order!", null, false);
        _tray.Clear();
        return ServeResult.Wrong;
    }

    private void UpdateTables(double dt)
    {
        for (int i = 0; i < TableCount; i++)
        {
            var customer = _tables[i];

            if (customer is null)
            {
                _spawnDelay[i] -= dt;
                if (_spawnDelay[i] <= 0) _tables[i] = SpawnCustomer();
                continue;
            }

            if (customer.State == CustomerState.Waiting)
            {
                customer.Patience -= dt;
                if (customer.Patience <= 0)
                {
                    customer.Patience = 0;
                    customer.State = CustomerState.Angry;
                    customer.LingerLeft = LingerSeconds;
                    AddPopup(i, "Too slow!", null, false);
                }
            }
            else
            {
                customer.LingerLeft -= dt;
                if (customer.LingerLeft <= 0)
                {
                    _tables[i] = null;
                    _spawnDelay[i] = 0.5 + _rng.NextDouble() * 1.1;
                }
            }
        }
    }

    private Customer SpawnCustomer()
    {
        var order = Order.CreateRandom(_elapsed, _rng);

        // Taller burgers get more time; patience shrinks a little as the round goes on.
        double patience = (7 + 2.6 * order.Count) * Math.Max(0.6, 1 - _elapsed / 150);

        return new Customer(
            order,
            _rng.Next(2) == 0 ? BodyShape.Square : BodyShape.Triangle,
            BodyColors[_rng.Next(BodyColors.Length)],
            SkinColors[_rng.Next(SkinColors.Length)],
            patience);
    }

    private void UpdatePopups(double dt)
    {
        foreach (var popup in _popups) popup.Age += dt;
        _popups.RemoveAll(p => p.Age > PopupSeconds);
    }

    private void AddPopup(int table, string text, string? subText, bool isGood) =>
        _popups.Add(new ScorePopup(++_popupId, table, text, subText, isGood));

    private void EndRound()
    {
        Phase = GamePhase.GameOver;
        IsNewBest = Score > HighScore;
        if (IsNewBest) HighScore = Score;
    }
}

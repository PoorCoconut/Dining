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
    public const double VipTimeBonusSeconds = 5;
    public const int TableCount = 3;
    public const int MaxTrayLayers = 14;

    private const double LingerSeconds = 0.8;
    private const double SmokeAtLingerLeft = 0.25;
    private const double PopupSeconds = 1.2;
    private const double BurstSeconds = 1.6;

    private const double VipChance = 0.10;
    private const double VipCooldownSeconds = 20;
    private const double VipEarliestSeconds = 8;

    private static readonly string[] BodyColors =
        { "#E4572E", "#2F9C95", "#7B5EA7", "#6BA43A", "#EF6F9B", "#3B6EA5" };

    private static readonly string[] SkinColors =
        { "#FFE0BD", "#F1C27D", "#D9A066", "#A8713F", "#FDEBD0" };

    private readonly Random _rng = new();
    private readonly List<IngredientType> _tray = new();
    private readonly List<ScorePopup> _popups = new();
    private readonly List<ParticleBurst> _bursts = new();
    private readonly Customer?[] _tables = new Customer?[TableCount];

    private double _elapsed;
    private double _spawnTimer;
    private double _lastVipAt;
    private int _popupId;
    private int _burstId;

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
    public IReadOnlyList<ParticleBurst> Bursts => _bursts;

    /// <summary>The restaurant fills up as the round goes on: 2 customers at first, up to 5.</summary>
    public int MaxActiveCustomers => Math.Min(TableCount, 2 + (int)(_elapsed / 15));

    public void Start()
    {
        Phase = GamePhase.Playing;
        TimeLeft = RoundSeconds;
        Score = 0;
        ServedCount = 0;
        IsNewBest = false;
        _elapsed = 0;
        _spawnTimer = 0.3;
        _lastVipAt = -999;
        _tray.Clear();
        _popups.Clear();
        _bursts.Clear();

        for (int i = 0; i < TableCount; i++) _tables[i] = null;
    }

    /// <summary>Advance the simulation by dt seconds.</summary>
    public void Tick(double dt)
    {
        if (Phase != GamePhase.Playing) return;

        dt = Math.Clamp(dt, 0, 0.25); // a backgrounded tab must not skip the whole round
        _elapsed += dt;
        TimeLeft -= dt;

        UpdateTables(dt);
        UpdateEffects(dt);

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
            double timeBonus = TimeBonusSeconds;

            if (customer.IsVip)
            {
                points *= 2;
                timeBonus = VipTimeBonusSeconds;
            }

            Score += points;
            ServedCount++;
            TimeLeft += timeBonus;
            customer.State = CustomerState.Happy;

            AddPopup(table, $"+{points}", $"+{(int)timeBonus}s", true);
            AddBurst(table,
                customer.PatienceFraction >= 0.5 ? ParticleKind.Delight : ParticleKind.Satisfied,
                customer.IsVip);

            _tray.Clear();
            return ServeResult.Correct;
        }

        customer.State = CustomerState.Angry;
        AddPopup(table, "Wrong order!", null, false);
        AddBurst(table, ParticleKind.Upset, false);
        _tray.Clear();
        return ServeResult.Wrong;
    }

    private void UpdateTables(double dt)
    {
        for (int i = 0; i < TableCount; i++)
        {
            var customer = _tables[i];
            if (customer is null) continue;

            if (customer.State == CustomerState.Waiting)
            {
                customer.Patience -= dt;
                if (customer.Patience <= 0)
                {
                    customer.Patience = 0;
                    customer.State = CustomerState.Angry;
                    customer.LingerLeft = LingerSeconds;
                    AddPopup(i, "Too slow!", null, false);
                    AddBurst(i, ParticleKind.Upset, false);
                }
                continue;
            }

            customer.LingerLeft -= dt;

            if (!customer.SmokeSpawned && customer.LingerLeft <= SmokeAtLingerLeft)
            {
                customer.SmokeSpawned = true;
                AddBurst(i, ParticleKind.Smoke, customer.IsVip);
            }

            if (customer.LingerLeft <= 0)
            {
                _tables[i] = null;
                _spawnTimer = Math.Max(_spawnTimer, 0.4 + _rng.NextDouble() * 0.9);
            }
        }

        _spawnTimer -= dt;
        if (_spawnTimer <= 0 && _tables.Count(c => c is not null) < MaxActiveCustomers)
        {
            SpawnCustomer();
            _spawnTimer = 0.6 + _rng.NextDouble() * 1.2;
        }
    }

    private void SpawnCustomer()
    {
        var free = Enumerable.Range(0, TableCount).Where(i => _tables[i] is null).ToList();
        if (free.Count == 0) return;
        int table = free[_rng.Next(free.Count)];

        bool vip =
            _elapsed >= VipEarliestSeconds &&
            _elapsed - _lastVipAt >= VipCooldownSeconds &&
            !_tables.Any(c => c is { IsVip: true }) &&
            _rng.NextDouble() < VipChance;

        Order order;
        double patience;

        if (vip)
        {
            order = Order.CreateMassive(_rng);
            patience = 16 + 1.7 * order.Count; // a long timer, but everyone else keeps waiting
            _lastVipAt = _elapsed;
            AddPopup(table, "VIP order!", null, true);
        }
        else
        {
            order = Order.CreateRandom(_elapsed, _rng);

            // Taller burgers get more time; patience shrinks a little as the round goes on.
            patience = (7 + 2.6 * order.Count) * Math.Max(0.6, 1 - _elapsed / 150);
        }

        _tables[table] = new Customer(
            order,
            _rng.Next(2) == 0 ? BodyShape.Square : BodyShape.Triangle,
            BodyColors[_rng.Next(BodyColors.Length)],
            SkinColors[_rng.Next(SkinColors.Length)],
            patience,
            vip);
    }

    private void UpdateEffects(double dt)
    {
        foreach (var popup in _popups) popup.Age += dt;
        _popups.RemoveAll(p => p.Age > PopupSeconds);

        foreach (var burst in _bursts) burst.Age += dt;
        _bursts.RemoveAll(b => b.Age > BurstSeconds);
    }

    private void AddPopup(int table, string text, string? subText, bool isGood) =>
        _popups.Add(new ScorePopup(++_popupId, table, text, subText, isGood));

    private void AddBurst(int table, ParticleKind kind, bool big) =>
        _bursts.Add(new ParticleBurst(++_burstId, table, kind, big));

    private void EndRound()
    {
        Phase = GamePhase.GameOver;
        IsNewBest = Score > HighScore;
        if (IsNewBest) HighScore = Score;
    }
}

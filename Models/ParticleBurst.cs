namespace Dining.Models;

public enum ParticleKind
{
    /// <summary>Customer is delighted: hearts, stars and confetti.</summary>
    Delight,

    /// <summary>Served, but slowly: a few relieved sparkles and sweat drops.</summary>
    Satisfied,

    /// <summary>Wrong order or ran out of patience: anger marks and tears.</summary>
    Upset,

    /// <summary>Puff of smoke as a customer leaves.</summary>
    Smoke
}

/// <summary>A short-lived group of particles shown at a table.</summary>
public sealed class ParticleBurst
{
    public int Id { get; }
    public int Table { get; }
    public ParticleKind Kind { get; }

    /// <summary>Bigger celebration (used for VIP customers).</summary>
    public bool Big { get; }

    public double Age { get; set; }

    public ParticleBurst(int id, int table, ParticleKind kind, bool big)
    {
        Id = id;
        Table = table;
        Kind = kind;
        Big = big;
    }
}

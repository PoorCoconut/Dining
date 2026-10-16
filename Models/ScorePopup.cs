namespace Dining.Models;

/// <summary>Short-lived floating text ("+250 / +1s", "Wrong order!") shown at a table.</summary>
public sealed class ScorePopup
{
    public int Id { get; }
    public int Table { get; }
    public string Text { get; }
    public string? SubText { get; }
    public bool IsGood { get; }
    public double Age { get; set; }

    public ScorePopup(int id, int table, string text, string? subText, bool isGood)
    {
        Id = id;
        Table = table;
        Text = text;
        SubText = subText;
        IsGood = isGood;
    }
}

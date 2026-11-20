using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Dining.Models;

[Table("leaderboard")]
public class LeaderboardEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("player_name")]
    public string PlayerName { get; set; } = "";

    [Column("score")]
    public int Score { get; set; }

    [Column("burgers_served")]
    public int BurgersServed { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}
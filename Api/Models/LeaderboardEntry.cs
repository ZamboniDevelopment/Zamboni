namespace Zamboni.Models;

public class LeaderboardEntry
{
    public string Gamertag { get; set; } = string.Empty;
    public int TotalGoals { get; set; }
    public int GamesPlayed { get; set; }
    public int Rank { get; set; }
}
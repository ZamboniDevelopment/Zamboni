namespace Zamboni.Models;

public class PlayerProfile
{
    public int UserId { get; set; }
    public string PlayerName { get; set; }
    public int TotalGames { get; set; }
    public int TotalGoals { get; set; }

    public PlayerProfile(int userId, string playerName, int totalGames, int totalGoals)
    {
        UserId = userId;
        PlayerName = playerName;
        TotalGames = totalGames;
        TotalGoals = totalGoals;
    }
}
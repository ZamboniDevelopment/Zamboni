using System;

namespace Zamboni.Models;

public class Game
{
    public int GameId { get; set; }
    public DateTime PlayedAt { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}
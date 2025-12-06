namespace Zamboni.Models;

public class ReportEntry
{
    public int UserId { get; set; }
    public string Gamertag { get; set; } = string.Empty;
    public int Score { get; set; }
}
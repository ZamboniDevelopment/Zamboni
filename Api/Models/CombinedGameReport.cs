using System.Collections.Generic;

namespace Zamboni.Models;

public class CombinedGameReport
{
    public int GameId { get; set; }
    public List<ReportEntry> Reports { get; set; } = new();
}
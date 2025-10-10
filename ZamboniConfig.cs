namespace Zamboni;

public class ZamboniConfig
{
    public string GameServerIp { get; set; } = "auto";
    public ushort GameServerPort { get; set; } = 13337;
    public string LogLevel { get; set; } = "Debug";
}
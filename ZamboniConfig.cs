namespace Zamboni;

public class ZamboniConfig
{
    public string GameServerIp { get; set; } = "auto"; //"auto" or manual public/local ip for GameServer server
    public ushort GameServerPort { get; set; } = 13337;
    public string LogLevel { get; set; } = "Debug";
}
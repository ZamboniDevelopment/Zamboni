namespace Zamboni;

public class ZamboniConfig
{
    public string GameServerIp { get; set; } = "auto";
    public ushort GameServerPort { get; set; } = 13337;
    public string LogLevel { get; set; } = "Debug";
    public string DatabaseConnectionString { get; set; } = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=zamboni";
}
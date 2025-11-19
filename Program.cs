using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;
using NLog;
using NLog.Layouts;
using Tdf;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Zamboni.Components.Blaze;
using Zamboni.Components.NHL10;

namespace Zamboni;

//((ip.src == 127.0.0.1) && (ip.dst == 127.0.0.1)) && tcp.port == 42100 || tcp.port == 13337 || tcp.port == 8999 || tcp.port == 9946 || tcp.port == 17502 || tcp.port == 17501 || tcp.port == 17500 || tcp.port == 17499
// (ip.dst == 192.168.100.178 && ip.src == 192.168.1.79) || (ip.src == 192.168.100.178 && ip.dst == 192.168.1.79)
//tcp.port == 8999 || tcp.port == 9946 || tcp.port == 17502 || tcp.port == 17501 || tcp.port == 17500 || tcp.port == 17499 || udp.port == 8999 || udp.port == 9946 || udp.port == 17502 || udp.port == 17501 || udp.port == 17500 || udp.port == 17499
//tcp.port == 17499 || udp.port == 17499 || tcp.port == 3659|| udp.port == 3659
// tcp.port == 17499 || udp.port == 17499 || tcp.port == 3659 || udp.port == 3659  || tcp.port == 17500 || udp.port == 17500 || tcp.port == 17501 || udp.port == 17501 || tcp.port == 17502 || udp.port == 17502 || tcp.port == 17503 || udp.port == 17503
internal class Program
{
    public const string Name = "Zamboni 1.3.4";
    public const int RedirectorPort = 42100;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static ZamboniConfig ZamboniConfig;
    public static Database Database;

    private static readonly string PublicIp = new HttpClient().GetStringAsync("https://checkip.amazonaws.com/").GetAwaiter().GetResult().Trim();
    public static string GameServerIp;

    private static async Task Main(string[] args)
    {
        InitConfig();
        StartLogger();
        InitDatabase();
        GameServerIp = ZamboniConfig.GameServerIp.Equals("auto") ? PublicIp : ZamboniConfig.GameServerIp;

        var commandTask = Task.Run(StartCommandListener);
        var redirectorTask = StartRedirectorServer();
        var coreTask = StartCoreServer();
        var apiTask = new Api().StartAsync();
        Logger.Warn(Name + " started");
        await Task.WhenAll(redirectorTask, coreTask, commandTask, apiTask);
    }

    private static void StartLogger()
    {
        var logLevel = LogLevel.FromString(ZamboniConfig.LogLevel);
        var layout = new SimpleLayout("[${longdate}][${callsite-filename:includeSourcePath=false}(${callsite-linenumber})][${level:uppercase=true}]: ${message:withexception=true}");
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(logLevel)
                .WriteToConsole(layout)
                .WriteToFile("logs/server-${shortdate}.log", layout);
        });
    }

    private static void InitConfig()
    {
        const string configFile = "zamboni-config.yml";
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        if (!File.Exists(configFile))
        {
            ZamboniConfig = new ZamboniConfig();
            var yaml = serializer.Serialize(ZamboniConfig);

            var comments = "# GameServerIp: 'auto' = automatically detect public IP or specify a manual IP address, where GameServer is run on\n" +
                           "# GameServerPort: Port for GameServer to listen on. (Redirector server lives on " + RedirectorPort + ", clients request there)\n" +
                           "# LogLevel: Valid values: Trace, Debug, Info, Warn, Error, Fatal, Off.\n" +
                           "# DatabaseConnectionString: Connection string to PostgreSQL, for saving data. (Not required)\n\n";
            File.WriteAllText(configFile, comments + yaml);
            Logger.Warn("Config file created: " + configFile);
            return;
        }

        var yamlText = File.ReadAllText(configFile);
        ZamboniConfig = deserializer.Deserialize<ZamboniConfig>(yamlText);
    }

    private static void InitDatabase()
    {
        Database = new Database();
    }

    private static async Task StartRedirectorServer()
    {
        var redirector = Blaze2.CreateBlazeServer("RedirectorServer", new IPEndPoint(IPAddress.Any, RedirectorPort));
        redirector.AddComponent<RedirectorComponent>();
        await redirector.Start(-1).ConfigureAwait(false);
    }

    private static async Task StartCoreServer()
    {
        var tdfFactory = new TdfFactory();
        var config = new BlazeServerConfiguration("CoreServer", new IPEndPoint(IPAddress.Any, ZamboniConfig.GameServerPort), tdfFactory.CreateLegacyEncoder(), tdfFactory.CreateLegacyDecoder());
        var core = new ZamboniCoreServer(config);
        core.AddComponent<UtilComponent>();
        core.AddComponent<AuthenticationComponent>();
        core.AddComponent<UserSessionsComponent>();
        core.AddComponent<MessagingComponent>();
        core.AddComponent<CensusDataComponent>();
        core.AddComponent<RoomsComponent>();
        core.AddComponent<LeagueComponent>();
        core.AddComponent<ClubsComponent>();
        core.AddComponent<StatsComponent>();
        core.AddComponent<GameManager>();
        core.AddComponent<GameReportingComponent>();

        core.AddComponent<DynamicMessagingComponent>(); // Seems to be NHL10 Specific Components
        core.AddComponent<OsdkSettingsComponent>(); // Seems to be NHL10 Specific Components

        await core.Start(-1).ConfigureAwait(false);
    }

    private static void StartCommandListener()
    {
        Logger.Info("Type 'help' or 'status'.");

        while (true)
        {
            var input = ReadLine.Read();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            switch (input.Trim().ToLowerInvariant())
            {
                case "help":
                    Logger.Warn("Available commands: help, status");
                    break;

                case "status":
                    Logger.Info(Name);
                    Logger.Info("Server running on ip: " + GameServerIp + " (" + PublicIp + ")");
                    Logger.Info("GameServerPort port: " + ZamboniConfig.GameServerPort);
                    Logger.Info("Redirector port: " + RedirectorPort);
                    Logger.Info("Online Players: " + ServerManager.GetServerPlayers().Count);
                    foreach (var serverPlayer in ServerManager.GetServerPlayers()) Logger.Info(serverPlayer.UserIdentification.mName);
                    Logger.Info("Queued Total Players: " + ServerManager.GetQueuedPlayers().Count);
                    foreach (var queuedPlayer in ServerManager.GetQueuedPlayers()) Logger.Info(queuedPlayer.ServerPlayer.UserIdentification.mName);
                    Logger.Info("Server Games: " + ServerManager.GetServerGames().Count);
                    foreach (var serverGame in ServerManager.GetServerGames()) Logger.Info(serverGame);
                    break;

                default:
                    Logger.Info($"Unknown command: {input}");
                    break;
            }
        }
    }
}
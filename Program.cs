using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;
using NLog;
using NLog.Layouts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Zamboni.Components.Blaze;
using Zamboni.Components.NHL10;

namespace Zamboni
{
    //((ip.src == 127.0.0.1) && (ip.dst == 127.0.0.1)) && tcp.port == 42100 || tcp.port == 13337 || tcp.port == 8999 || tcp.port == 9946 || tcp.port == 17502 || tcp.port == 17501 || tcp.port == 17500 || tcp.port == 17499
    // (ip.dst == 192.168.100.178 && ip.src == 192.168.1.79) || (ip.src == 192.168.100.178 && ip.dst == 192.168.1.79)
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static ZamboniConfig ZamboniConfig;
        public static List<HockeyUser> HockeyUsers = new List<HockeyUser>();

        public static string MachineIP;

        public static HockeyUser getHockeyUser(BlazeServerConnection blazeServerConnection)
        {
            HockeyUser hockeyUser = null;
            foreach (HockeyUser loopUser in HockeyUsers)
            {
                if (loopUser.BlazeServerConnection.Equals(blazeServerConnection))
                {
                    hockeyUser = loopUser;
                    break;
                }
            }

            if (hockeyUser == null) Logger.Warn("huh?");

            return hockeyUser;
        }

        static async Task Main(string[] args)
        {
            InitConfig();
            StartLogger();
            MachineIP = new HttpClient().GetStringAsync("https://checkip.amazonaws.com/").GetAwaiter().GetResult()
                .Trim();

            var redirectorTask = StartRedirectorServer();
            var coreTask = StartCoreServer();
            var commandTask = Task.Run(StartCommandListener);
            Logger.Warn("Zamboni server started");
            await Task.WhenAll(redirectorTask, coreTask, commandTask);
        }

        private static void StartLogger()
        {
            LogLevel logLevel = LogLevel.FromString(ZamboniConfig.LogLevel);
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(logLevel)
                    .WriteToConsole(new SimpleLayout(
                        "[${longdate}][${callsite-filename:includeSourcePath=false}(${callsite-linenumber})][${level:uppercase=true}]: ${message:withexception=true}"));
            });
        }

        private static void InitConfig()
        {
            var configFile = "zamboni-config.yml";
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            if (!File.Exists(configFile))
            {
                ZamboniConfig = new ZamboniConfig();
                var yaml = serializer.Serialize(ZamboniConfig);

                var comments =
                    "# Setting GameServerIp: 'auto' = automatically detect public IP or specify a manual IP address.\n" +
                    "# Setting GameServerPort: Port for GameServer to listen on.(Also open port 42100 on your host)\n" +
                    "# Setting LogLevel: Valid values: Trace, Debug, Info, Warn, Error, Fatal, Off.\n\n";
                File.WriteAllText(configFile, comments + yaml);
                Logger.Warn("Config file created: " + configFile);
                return;
            }

            var yamlText = File.ReadAllText(configFile);
            ZamboniConfig = deserializer.Deserialize<ZamboniConfig>(yamlText);
        }

        private static async Task StartRedirectorServer()
        {
            BlazeServer redirector = Blaze2.CreateBlazeServer("RedirectorServer", new IPEndPoint(IPAddress.Any, 42100));
            redirector.AddComponent<RedirectorComponent>();
            await redirector.Start(-1).ConfigureAwait(false);
        }

        private static async Task StartCoreServer()
        {
            BlazeServer core = Blaze2.CreateBlazeServer("CoreServer",
                new IPEndPoint(IPAddress.Any, ZamboniConfig.GameServerPort));
            core.AddComponent<UtilComponent>();
            core.AddComponent<AuthenticationComponent>();
            core.AddComponent<UserSessionsComponent>();
            core.AddComponent<MessagingComponent>();
            core.AddComponent<CensusDataComponent>();
            core.AddComponent<RoomsComponent>();
            core.AddComponent<LeagueComponent>();
            core.AddComponent<ClubsComponent>();
            core.AddComponent<StatsComponent>();
            core.AddComponent<GameManagerComponent>();

            core.AddComponent<DynamicMessagingComponent>(); // Seems to be NHL10 Specific Components
            core.AddComponent<OsdkSettingsComponent>(); // Seems to be NHL10 Specific Components

            await core.Start(-1).ConfigureAwait(false);
        }

        private static void StartCommandListener()
        {
            Logger.Warn("Type 'help' or 'status'.");

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
                        Logger.Warn(
                            "Server running on ip: " + ZamboniConfig.GameServerIp + " (" + MachineIP + ")");
                        Logger.Warn("GameServerPort port: " + ZamboniConfig.GameServerPort);
                        Logger.Warn("Redirector port: 42100");
                        Logger.Warn("Connected Users: " + HockeyUsers.Count);
                        foreach (var user in HockeyUsers)
                        {
                            Logger.Warn(user.username + ", ");
                        }

                        Logger.Warn("Queued Users: " + GameManagerComponent.QueuedHockeyUsers.Count);
                        foreach (var kv in GameManagerComponent.QueuedHockeyUsers)
                        {
                            Logger.Warn(kv.Key.username + ", ");
                        }

                        break;

                    default:
                        Logger.Warn($"Unknown command: {input}");
                        break;
                }
            }
        }
    }
}
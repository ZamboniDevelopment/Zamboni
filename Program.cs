using System.Net;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;
using NLog;
using NLog.Layouts;
using Zamboni.Components.Blaze;
using Zamboni.Components.NHL10;

namespace Zamboni
{
    //((ip.src == 127.0.0.1) && (ip.dst == 127.0.0.1)) && tcp.port == 42100 || tcp.port == 13337 || tcp.port == 8999 || tcp.port == 9946 || tcp.port == 17502 || tcp.port == 17501 || tcp.port == 17500 || tcp.port == 17499
    class Program
    {
        static async Task Main(string[] args)
        {
            StartLogger();
            await Task.WhenAll(StartRedirectorServer(), StartCoreServer());
        }

        private static void StartLogger()
        {
            LogLevel logLevel = LogLevel.Debug;
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(logLevel)
                    .WriteToConsole(new SimpleLayout("[${longdate}][${callsite-filename:includeSourcePath=false}(${callsite-linenumber})][${level:uppercase=true}]: ${message:withexception=true}"));
            });
        }

        private static async Task StartRedirectorServer()
        {
            BlazeServer redirector = Blaze2.CreateBlazeServer("gosredirector.ea.com", new IPEndPoint(IPAddress.Any, 42100));
            redirector.AddComponent<RedirectorComponent>();
            await redirector.Start(-1).ConfigureAwait(false);
        }

        private static async Task StartCoreServer()
        {
            BlazeServer core = Blaze2.CreateBlazeServer("localhost", new IPEndPoint(IPAddress.Any, 13337));
            core.AddComponent<UtilComponent>();
            core.AddComponent<AuthenticationComponent>();
            core.AddComponent<UserSessionsComponent>();
            core.AddComponent<MessagingComponent>();
            core.AddComponent<CensusDataComponent>();
            core.AddComponent<RoomsComponent>();
            core.AddComponent<LeagueComponent>();
            core.AddComponent<ClubsComponent>();
            core.AddComponent<StatsComponent>();

            core.AddComponent<DynamicMessagingComponent>(); // Seems to be NHL10 Specific Components
            core.AddComponent<OsdkSettingsComponent>(); // Seems to be NHL10 Specific Components
            
            await core.Start(-1).ConfigureAwait(false);
        }
    }
    
}
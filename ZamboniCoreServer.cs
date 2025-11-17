using System.Threading.Tasks;
using BlazeCommon;

namespace Zamboni;

public class ZamboniCoreServer : BlazeServer
{
    public ZamboniCoreServer(BlazeServerConfiguration settings) : base(settings)
    {
    }

    public override Task OnProtoFireDisconnectAsync(ProtoFireConnection connection)
    {
        var serverPlayer = ServerManager.GetServerPlayer(connection);
        if (serverPlayer == null) return base.OnProtoFireDisconnectAsync(connection);
        ServerManager.RemoveServerPlayer(serverPlayer);

        var queuedPlayer = ServerManager.GetQueuedPlayer(serverPlayer);
        if (queuedPlayer != null) ServerManager.RemoveQueuedPlayer(queuedPlayer);

        var serverGame = ServerManager.GetServerGame(serverPlayer);
        if (serverGame != null) serverGame.RemoveGameParticipant(serverPlayer);

        return base.OnProtoFireDisconnectAsync(connection);
    }
}
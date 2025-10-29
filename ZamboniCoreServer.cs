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
        var zamboniUser = Manager.GetZamboniUser(connection);
        if (zamboniUser == null) return base.OnProtoFireDisconnectAsync(connection);
        Manager.ZamboniUsers.Remove(zamboniUser);

        var queuedUser = Manager.GetQueuedUser(zamboniUser);
        if (queuedUser != null) Manager.QueuedUsers.Remove(queuedUser);

        var zamboniGame = Manager.GetZamboniGame(zamboniUser);
        if (zamboniGame != null) zamboniGame.RemoveGameParticipant(zamboniUser);

        return base.OnProtoFireDisconnectAsync(connection);
    }
}
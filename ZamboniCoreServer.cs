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
        Manager.QueuedMatchZamboniUsers.Remove(zamboniUser);
        Manager.QueuedShootoutZamboniUsers.Remove(zamboniUser);

        var zamboniGame = Manager.GetZamboniGame(zamboniUser);
        if (zamboniGame == null) return base.OnProtoFireDisconnectAsync(connection);

        zamboniGame.RemoveGameParticipant(zamboniUser);

        return base.OnProtoFireDisconnectAsync(connection);
    }
}
using System.Threading.Tasks;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;
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
        Manager.QueuedZamboniUsers.Remove(zamboniUser);

        var zamboniGame = Manager.GetZamboniGame(zamboniUser);
        if (zamboniGame == null) return base.OnProtoFireDisconnectAsync(connection);

        foreach (var loopUser in zamboniGame.ZamboniUsers)
        {
            GameManagerBase.Server.NotifyPlayerRemovedAsync(loopUser.BlazeServerConnection, new NotifyPlayerRemoved
            {
                mPlayerRemovedTitleContext = 0,
                mGameId = zamboniGame.GameId,
                mPlayerId = (uint)zamboniGame.ZamboniUsers[0].UserId,
                mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
            });
            GameManagerBase.Server.NotifyPlayerRemovedAsync(loopUser.BlazeServerConnection, new NotifyPlayerRemoved
            {
                mPlayerRemovedTitleContext = 0,
                mGameId = zamboniGame.GameId,
                mPlayerId = (uint)zamboniGame.ZamboniUsers[1].UserId,
                mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
            });
        }

        Manager.ZamboniGames.Remove(zamboniGame);

        return base.OnProtoFireDisconnectAsync(connection);
    }
}
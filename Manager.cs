using System.Collections.Generic;
using System.Linq;
using BlazeCommon;

namespace Zamboni;

public static class Manager
{
    public static readonly HashSet<ZamboniUser> ZamboniUsers = new();

    public static readonly HashSet<QueuedUser> QueuedUsers = new();

    public static readonly HashSet<ZamboniGame> ZamboniGames = new();

    public static ZamboniUser GetZamboniUser(BlazeServerConnection blazeServerConnection)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.BlazeServerConnection.Equals(blazeServerConnection));
    }

    public static ZamboniUser GetZamboniUser(ProtoFireConnection protoFireConnection)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.BlazeServerConnection.ProtoFireConnection.Equals(protoFireConnection));
    }

    public static ZamboniUser GetZamboniUser(uint userId)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.UserId.Equals(userId));
    }

    public static ZamboniUser GetZamboniUser(ulong messengerId)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.MessengerId.Equals(messengerId));
    }

    public static ZamboniUser GetZamboniUser(string name)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.Username.Equals(name));
    }

    public static ZamboniGame GetZamboniGame(uint id)
    {
        return ZamboniGames.FirstOrDefault(loopGame => loopGame.ReplicatedGameData.mGameId.Equals(id));
    }

    public static ZamboniGame GetZamboniGame(ZamboniUser zamboniUser)
    {
        return ZamboniGames.FirstOrDefault(loopGame => loopGame.ZamboniUsers.Contains(zamboniUser));
    }

    public static QueuedUser GetQueuedUser(ZamboniUser zamboniUser)
    {
        return QueuedUsers.FirstOrDefault(loopUser => loopUser.ZamboniUser.Equals(zamboniUser));
    }
}
using System.Collections.Generic;
using System.Linq;
using BlazeCommon;

namespace Zamboni;

public static class Manager
{
    public static readonly List<ZamboniUser> ZamboniUsers = new();
    public static readonly List<ZamboniUser> QueuedZamboniUsers = new();

    public static readonly List<ZamboniGame> ZamboniGames = new();

    public static ZamboniUser GetZamboniUser(BlazeServerConnection blazeServerConnection)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.BlazeServerConnection.Equals(blazeServerConnection));
    }

    public static ZamboniUser GetZamboniUser(ProtoFireConnection protoFireConnection)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.BlazeServerConnection.ProtoFireConnection.Equals(protoFireConnection));
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
}
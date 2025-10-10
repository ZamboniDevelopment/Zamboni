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

    public static ZamboniUser GetZamboniUser(string name)
    {
        return ZamboniUsers.FirstOrDefault(loopUser => loopUser.Username.Equals(name));
    }

    public static ZamboniGame GetZamboniGame(uint id)
    {
        return ZamboniGames.FirstOrDefault(loopGame => loopGame.ReplicatedGameData.mGameId.Equals(id));
    }
}
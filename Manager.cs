using System.Collections.Generic;
using System.Linq;
using BlazeCommon;

namespace Zamboni;

public static class Manager
{
    public static readonly List<HockeyUser> HockeyUsers = new();
    public static readonly List<HockeyUser> QueuedHockeyUsers = new();

    public static HockeyUser GetHockeyUser(BlazeServerConnection blazeServerConnection)
    {
        return HockeyUsers.FirstOrDefault(loopUser => loopUser.BlazeServerConnection.Equals(blazeServerConnection));
    }

    public static HockeyUser GetHockeyUser(string name)
    {
        return HockeyUsers.FirstOrDefault(loopUser => loopUser.Username.Equals(name));
    }
}
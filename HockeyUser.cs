using Blaze2SDK.Blaze;
using BlazeCommon;

namespace Zamboni;

public class HockeyUser
{
    public BlazeServerConnection BlazeServerConnection;
    public ulong userId;
    public string username;
    public NetworkAddress NetworkAddress;

    public HockeyUser(BlazeServerConnection BlazeServerConnection, ulong userId, string username)
    {
        this.BlazeServerConnection = BlazeServerConnection;
        this.userId = userId;
        this.username = username;
    }
}
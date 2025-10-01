using Blaze2SDK.Blaze;
using BlazeCommon;

namespace Zamboni;

public class HockeyUser
{
    public HockeyUser(BlazeServerConnection blazeServerConnection, ulong userId, string username)
    {
        BlazeServerConnection = blazeServerConnection;
        UserId = userId;
        Username = username;
    }

    public NetworkAddress NetworkAddress { get; set; }
    public BlazeServerConnection BlazeServerConnection { get; }
    public ulong UserId { get; }
    public string Username { get; }
}
using System.Collections.Generic;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;
using BlazeCommon;

namespace Zamboni;

public class ZamboniUser
{
    public ZamboniUser(BlazeServerConnection blazeServerConnection, ulong userId, string username)
    {
        BlazeServerConnection = blazeServerConnection;
        UserId = userId;
        Username = username;
    }

    public NetworkInfo NetworkInfo { get; set; }
    public BlazeServerConnection BlazeServerConnection { get; }
    public ulong UserId { get; }
    public string Username { get; }

    public ReplicatedGamePlayer ToReplicatedGamePlayer(byte slot, uint gameId)
    {
        return new ReplicatedGamePlayer
        {
            mCustomData = new byte[]
            {
            },
            mExternalId = UserId,
            mGameId = gameId,
            mAccountLocale = 1701729619,
            mPlayerName = Username,
            mNetworkQosData = NetworkInfo.mQosData,
            mPlayerAttribs = new SortedDictionary<string, string>(),
            mPlayerId = (uint)UserId,
            mNetworkAddress = NetworkInfo.mAddress,
            mSlotId = slot,
            mSlotType = SlotType.SLOT_PRIVATE,
            mPlayerState = PlayerState.ACTIVE_CONNECTING,
            mPlayerSessionId = (uint)UserId //TODO ????
        };
    }
}
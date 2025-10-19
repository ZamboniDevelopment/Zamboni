using System.Collections.Generic;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;
using BlazeCommon;

namespace Zamboni;

public class ZamboniUser
{
    private const ulong MessengerPrefix = 0x7802000100000000;

    public ZamboniUser(BlazeServerConnection blazeServerConnection, ulong userId, string username, byte[] externalBlob)
    {
        BlazeServerConnection = blazeServerConnection;
        UserId = userId;
        Username = username;
        MessengerId = MessengerPrefix | userId;
        ExternalBlob = externalBlob;
        Manager.ZamboniUsers.Add(this);
    }

    public NetworkInfo NetworkInfo { get; set; }
    public BlazeServerConnection BlazeServerConnection { get; }
    public ulong UserId { get; }
    public string Username { get; }

    public byte[] ExternalBlob { get; }

    public ulong MessengerId { get; }

    public ReplicatedGamePlayer ToReplicatedGamePlayer(byte slot, uint gameId)
    {
        return new ReplicatedGamePlayer
        {
            mCustomData = ExternalBlob,
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
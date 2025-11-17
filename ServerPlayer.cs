using System.Collections.Generic;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.Authentication;
using Blaze2SDK.Blaze.GameManager;
using BlazeCommon;

namespace Zamboni;

public class ServerPlayer
{
    private const ulong MessengerPrefix = 0x7802000100000000;

    public ServerPlayer(BlazeServerConnection blazeServerConnection, UserIdentification userIdentification, UserSessionExtendedData extendedData, SessionInfo sessionInfo)
    {
        BlazeServerConnection = blazeServerConnection;
        UserIdentification = userIdentification;
        ExtendedData = extendedData;
        SessionInfo = sessionInfo;
        MessengerId = MessengerPrefix | userIdentification.mExternalId;
        ServerManager.AddServerPlayer(this);
    }

    public BlazeServerConnection BlazeServerConnection { get; }
    public UserIdentification UserIdentification { get; set; }
    public UserSessionExtendedData ExtendedData { get; set; }
    public SessionInfo SessionInfo { get; set; }
    public ulong MessengerId { get; }

    public ReplicatedGamePlayer ToReplicatedGamePlayer(byte slot, uint gameId)
    {
        return new ReplicatedGamePlayer
        {
            mCustomData = UserIdentification.mExternalBlob,
            mExternalId = UserIdentification.mExternalId,
            mGameId = gameId,
            mAccountLocale = 1701729619,
            mPlayerName = UserIdentification.mName,
            mNetworkQosData = ExtendedData.mQosData,
            mPlayerAttribs = new SortedDictionary<string, string>(),
            mPlayerId = UserIdentification.mBlazeId,
            mNetworkAddress = ExtendedData.mAddress,
            mSlotId = slot,
            mSlotType = SlotType.SLOT_PRIVATE,
            mPlayerState = PlayerState.ACTIVE_CONNECTING,
            mPlayerSessionId = UserIdentification.mBlazeId //TODO ????
        };
    }
}
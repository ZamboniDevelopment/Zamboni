using System.Collections.Generic;
using System.Threading.Tasks;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

public class GameManagerComponent : GameManagerBase.Server
{
    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        Task.Run(async () =>
        {
            await Task.Delay(10);

            NotifyMatchmakingFinishedAsync(context.BlazeConnection, new NotifyMatchmakingFinished
            {
                mFitScore = 10,
                mGameId = 0,
                mMaxPossibleFitScore = 10,
                mSessionId = 0,
                mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
                mUserSessionId = 0
            });
        });
        return Task.FromResult(new StartMatchmakingResponse()
        {
            mSessionId = 0,
        });
    }

    public override Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, BlazeRpcContext context)
    {


        ReplicatedGameData gameData = new ReplicatedGameData
        {
            
            mAdminPlayerList = request.mAdminPlayerList,
            mGameAttribs = request.mGameAttribs,
            mSlotCapacities = request.mSlotCapacities,
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameId = 0,
            mGameName = request.mGameName,
            mGameProtocolVersionHash = 0, //TODO
            mGameSettings = request.mGameSettings,
            mGameReportingId = 0,
            mGameState = GameState.NEW_STATE, //TODO
            mGameProtocolVersion = request.mGameProtocolVersion,
            mHostNetworkAddressList = request.mHostNetworkAddress,
            mTopologyHostSessionId = 0, //TODO
            mIgnoreEntryCriteriaWithInvite = false,
            mMeshAttribs = request.mMeshAttribs,
            mMaxPlayerCapacity = request.mMaxPlayerCapacity,
            mNetworkQosData = default, //TODO
            mNetworkTopology = GameNetworkTopology.PEER_TO_PEER_FULL_MESH, //TODO
            mPersistedGameId = request.mPersistedGameId,
            mPersistedGameIdSecret = request.mPersistedGameIdSecret,
            mPlatformHostInfo = default, //TODO
            mPingSiteAlias = request.mGamePingSiteAlias,
            mQueueCapacity = request.mQueueCapacity,
            mSharedSeed = 0, //TODO
            mTeamCapacities = request.mTeamCapacities,
            mTopologyHostInfo = default, //TODO
            mUUID = "",
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mXnetNonce = new byte[]
            {
                //TODO
            },
            mXnetSession = new byte[]
            {
                //TODO
            }
        };
        List<ReplicatedGamePlayer> repe = new List<ReplicatedGamePlayer>
        {
            new ReplicatedGamePlayer
            {
                mCustomData = new byte[]
                {
                },
                mExternalId = 0,
                mGameId = 0,
                mAccountLocale = 1701729619,
                mPlayerName = "Mojo991",
                mNetworkQosData = default,
                mPlayerAttribs = request.mHostPlayerAttribs,
                mPlayerId = 0,
                mNetworkAddress = request.mHostNetworkAddress,
                mSlotId = 0,
                mSlotType = request.mJoiningSlotType,
                mPlayerState = PlayerState.RESERVED,
                mTeamId = 0,
                mTeamIndex = 0,
                mJoinedGameTimestamp = 0,
                mPlayerSessionId = 0
            }
        };
        Task.Run(async () =>
        {
            await Task.Delay(10);
            NotifyGameCreatedAsync(context.BlazeConnection, new NotifyGameCreated
            {
                mGameId = 0
            });
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                NotifyJoinGameAsync(context.BlazeConnection, new NotifyJoinGame
                {
                    mJoinErr = 0,
                    mGameData = gameData,
                    mMatchmakingSessionId = 0,
                    mGameRoster = repe,
                });
            });
        });


        return Task.FromResult(new CreateGameResponse
        {
            mGameData = gameData,
            mGameId = 0,
            mHostId = 0,
            mGameRoster = repe,
        });
    }
    
    
}
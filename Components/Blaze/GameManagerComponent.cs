using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;
using BlazeCommon;
using NLog;

namespace Zamboni.Components.Blaze;

public class GameManagerComponent : GameManagerBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static uint _gameIdCounter;

    private static void Trigger()
    {
        if (Manager.QueuedHockeyUsers.Count < 2) return;
        var gameId = _gameIdCounter++;
        var hockeyUserA = Manager.QueuedHockeyUsers[0];
        var hockeyUserB = Manager.QueuedHockeyUsers[1];

        var rankedGameData = CreateReplicatedRankedGame(gameId, hockeyUserA);
        var replicatedGamePlayerA = CreateReplicatedGamePlayer(hockeyUserA, 0, gameId);
        var replicatedGamePlayerB = CreateReplicatedGamePlayer(hockeyUserB, 1, gameId);

        var replicatedGamePlayers = new List<ReplicatedGamePlayer>
        {
            replicatedGamePlayerA, replicatedGamePlayerB
        };

        NotifyMatchmakingFinishedAsync(hockeyUserA.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = gameId,
            mMaxPossibleFitScore = 10,
            mSessionId = (uint)hockeyUserA.UserId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
            mUserSessionId = (uint)hockeyUserA.UserId
        });
        NotifyMatchmakingFinishedAsync(hockeyUserB.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = gameId,
            mMaxPossibleFitScore = 10,
            mSessionId = (uint)hockeyUserB.UserId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_NEW_GAME,
            mUserSessionId = (uint)hockeyUserB.UserId
        });


        NotifyJoinGameAsync(hockeyUserA.BlazeServerConnection, new NotifyJoinGame
        {
            mJoinErr = 0,
            mGameData = rankedGameData,
            mMatchmakingSessionId = (uint)hockeyUserA.UserId,
            mGameRoster = replicatedGamePlayers
        });

        NotifyJoinGameAsync(hockeyUserB.BlazeServerConnection, new NotifyJoinGame
        {
            mJoinErr = 0,
            mGameData = rankedGameData,
            mMatchmakingSessionId = (uint)hockeyUserB.UserId,
            mGameRoster = replicatedGamePlayers
        });

        Manager.QueuedHockeyUsers.Remove(hockeyUserA);
        Manager.QueuedHockeyUsers.Remove(hockeyUserB);
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        var hockeyUser = Manager.GetHockeyUser(context.BlazeConnection);
        Logger.Warn(hockeyUser.Username + " queued");
        Manager.QueuedHockeyUsers.Add(hockeyUser);
        Trigger();
        return Task.FromResult(new StartMatchmakingResponse
        {
            mSessionId = (uint)Manager.GetHockeyUser(context.BlazeConnection).UserId
        });
    }

    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request, BlazeRpcContext context)
    {
        var hockeyUser = Manager.GetHockeyUser(context.BlazeConnection);
        Manager.QueuedHockeyUsers.Remove(hockeyUser);
        Logger.Warn(hockeyUser.Username + " unqueued");
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateGameSessionAsync(UpdateGameSessionRequest request,
        BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }


    public override Task<NullStruct> FinalizeGameCreationAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        NotifyGameSessionUpdatedAsync(context.BlazeConnection, new GameSessionUpdatedNotification
        {
            mGameId = request.mGameId,
            mXnetNonce = request.mXnetNonce,
            mXnetSession = request.mXnetSession
        });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> AdvanceGameStateAsync(AdvanceGameStateRequest request, BlazeRpcContext context)
    {
        NotifyGameStateChangeAsync(context.BlazeConnection, new NotifyGameStateChange
        {
            mGameId = request.mGameId,
            mNewGameState = request.mNewGameState
        });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> SetGameSettingsAsync(SetGameSettingsRequest request, BlazeRpcContext context)
    {
        NotifyGameSettingsChangeAsync(context.BlazeConnection, new NotifyGameSettingsChange
        {
            mGameSettings = request.mGameSettings,
            mGameId = request.mGameId
        });
        return Task.FromResult(new NullStruct());
    }

    private static ReplicatedGamePlayer CreateReplicatedGamePlayer(HockeyUser hockeyUser, byte slot, uint gameId)
    {
        return new ReplicatedGamePlayer
        {
            mCustomData = new byte[]
            {
            },
            mExternalId = hockeyUser.UserId,
            mGameId = gameId,
            mAccountLocale = 1701729619,
            mPlayerName = hockeyUser.Username,
            mNetworkQosData = default,
            mPlayerId = (uint)hockeyUser.UserId,
            mNetworkAddress = hockeyUser.NetworkAddress,
            mSlotId = slot,
            mSlotType = SlotType.SLOT_PRIVATE,
            mPlayerState = PlayerState.ACTIVE_CONNECTED,
            mPlayerSessionId = (uint)hockeyUser.UserId //TODO ????
        };
    }

    private static ReplicatedGameData CreateReplicatedRankedGame(uint gameId, HockeyUser host)
    {
        return new ReplicatedGameData
        {
            mAdminPlayerList = new List<uint>
            {
                (uint)host.UserId
            },
            mGameAttribs = new SortedDictionary<string, string>
            {
                {
                    "Rules", "1"
                },
                {
                    "PeriodLength", "5"
                },
                {
                    "Penalties", "1"
                },
                {
                    "OSDK_sponsoredEventId", "0"
                },
                {
                    "OSDK_roomId", "0"
                },
                {
                    "OSDK_matchupHash", "0"
                },
                {
                    "OSDK_gameMode", "1"
                },
                {
                    "Injuries", "1"
                },
                {
                    "Fighting", "1"
                },
                {
                    "CreatedPlays", "1"
                }
            },
            mSlotCapacities = new List<ushort>
            {
                0, 2
            }, //TODO
            mEntryCriteriaMap = new SortedDictionary<string, string>(),
            mGameId = gameId,
            mGameName = "game" + gameId,
            mGameProtocolVersionHash = GetGameProtocolVersionHash(),
            mGameSettings = GameSettings.Ranked,
            mGameReportingId = 0,
            mGameState = GameState.NEW_STATE,
            mGameProtocolVersion = 1,
            mHostNetworkAddress = host.NetworkAddress,
            mTopologyHostSessionId = (uint)host.UserId,
            mIgnoreEntryCriteriaWithInvite = false,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mMaxPlayerCapacity = 2,
            mNetworkQosData = default,
            mNetworkTopology = GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED,
            mPersistedGameId = gameId.ToString(),
            mPersistedGameIdSecret = new byte[]
            {
            },
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = (uint)host.UserId,
                mSlotId = 0
            },
            mPingSiteAlias = "qos",
            mQueueCapacity = 0,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = (uint)host.UserId,
                mSlotId = 0
            },
            mUUID = "game" + gameId,
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = "NHL10_1.00",
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            }
        };
    }

    // https://github.com/PocketRelay/Server/issues/59
    public static ulong GetGameProtocolVersionHash(string protocolVersion = "NHL10_1.00")
    {
        protocolVersion ??= string.Empty;
        //FNV1 HASH - the same hashing logic is used in ea blaze for game protocol versions
        var buf = Encoding.UTF8.GetBytes(protocolVersion);
        var hash = 2166136261UL;
        foreach (var c in buf)
            hash = (hash * 16777619) ^ c;
        return hash;
    }
}
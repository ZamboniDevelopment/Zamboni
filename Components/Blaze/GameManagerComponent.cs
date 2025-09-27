using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public static uint gameIDCounter = 0;
    public static uint searchIDCounter = 0;

    public static List<KeyValuePair<HockeyUser, uint>> QueuedHockeyUsers = new List<KeyValuePair<HockeyUser, uint>>();

    static GameManagerComponent()
    {
        new Timer(OnSecond, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private static void OnSecond(object? state)
    {
        if (QueuedHockeyUsers.Count >= 2)
        {
            uint gameID = gameIDCounter++;
            KeyValuePair<HockeyUser, uint> hockeyUserA = QueuedHockeyUsers[0];
            KeyValuePair<HockeyUser, uint> hockeyUserB = QueuedHockeyUsers[1];

            ReplicatedGameData rankedGameData = RankedGameData(gameID, hockeyUserA.Key.NetworkAddress);

            ReplicatedGamePlayer hockeyGamePlayerA = new ReplicatedGamePlayer
            {
                mCustomData = new byte[]
                {
                },
                mExternalId = hockeyUserA.Key.userId,
                mGameId = gameID,
                mAccountLocale = 1701729619,
                mPlayerName = hockeyUserA.Key.username,
                mNetworkQosData = default,
                mPlayerId = (uint)hockeyUserA.Key.userId,
                mNetworkAddress = hockeyUserA.Key.NetworkAddress,
                mSlotId = 0,
                mSlotType = SlotType.SLOT_PRIVATE,
                mPlayerState = PlayerState.ACTIVE_CONNECTED,
                mPlayerSessionId = hockeyUserA.Value //TODO ????
            };

            ReplicatedGamePlayer hockeyGamePlayerB = new ReplicatedGamePlayer
            {
                mCustomData = new byte[]
                {
                },
                mExternalId = hockeyUserB.Key.userId,
                mGameId = gameID,
                mAccountLocale = 1701729619,
                mPlayerName = hockeyUserB.Key.username,
                mNetworkQosData = default,
                mPlayerId = (uint)hockeyUserB.Key.userId,
                mNetworkAddress = hockeyUserB.Key.NetworkAddress,
                mSlotId = 1,
                mSlotType = SlotType.SLOT_PRIVATE,
                mPlayerState = PlayerState.ACTIVE_CONNECTED,
                mPlayerSessionId = hockeyUserB.Value //TODO ????
            };

            List<ReplicatedGamePlayer> players = new List<ReplicatedGamePlayer>
            {
                hockeyGamePlayerA, hockeyGamePlayerB
            };

            NotifyMatchmakingFinishedAsync(hockeyUserA.Key.BlazeServerConnection, new NotifyMatchmakingFinished
            {
                mFitScore = 10,
                mGameId = gameID,
                mMaxPossibleFitScore = 10,
                mSessionId = hockeyUserA.Value,
                mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
                mUserSessionId = hockeyUserA.Value
            });
            NotifyMatchmakingFinishedAsync(hockeyUserB.Key.BlazeServerConnection, new NotifyMatchmakingFinished
            {
                mFitScore = 10,
                mGameId = gameID,
                mMaxPossibleFitScore = 10,
                mSessionId = hockeyUserB.Value,
                mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_NEW_GAME,
                mUserSessionId = hockeyUserB.Value
            });


            NotifyJoinGameAsync(hockeyUserA.Key.BlazeServerConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = rankedGameData,
                mMatchmakingSessionId = 0,
                mGameRoster = players
            });

            NotifyJoinGameAsync(hockeyUserB.Key.BlazeServerConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = rankedGameData,
                mMatchmakingSessionId = 0,
                mGameRoster = players
            });

            QueuedHockeyUsers.Remove(hockeyUserA);
            QueuedHockeyUsers.Remove(hockeyUserB);
        }
    }


    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request,
        BlazeRpcContext context)
    {
        KeyValuePair<HockeyUser, uint> qUser = default;
        foreach (var LQUser in QueuedHockeyUsers)
        {
            if (LQUser.Key.BlazeServerConnection.Equals(context.BlazeConnection))
            {
                Logger.Warn(LQUser.Key.username + " unqueued");
                qUser = LQUser;
                break;
            }
        }

        QueuedHockeyUsers.Remove(qUser);

        return Task.FromResult(new NullStruct()
        {
        });
    }

    public override Task<NullStruct> UpdateGameSessionAsync(UpdateGameSessionRequest request,
        BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
        });
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request,
        BlazeRpcContext context)
    {
        uint searchID = searchIDCounter++;
        Program.getHockeyUser(context.BlazeConnection).NetworkAddress = request.mPlayerNetworkAddress;
        Logger.Warn(Program.getHockeyUser(context.BlazeConnection).username + " queued");
        QueuedHockeyUsers.Add(
            new KeyValuePair<HockeyUser, uint>(Program.getHockeyUser(context.BlazeConnection), searchID));
        return Task.FromResult(new StartMatchmakingResponse()
        {
            mSessionId = searchID,
        });
    }
    // Task.Run(async () =>
    // {
    //     await Task.Delay(100);
    //
    //     NotifyMatchmakingFinishedAsync(context.BlazeConnection, new NotifyMatchmakingFinished
    //     {
    //         mFitScore = 10,
    //         mGameId = 0,
    //         mMaxPossibleFitScore = 10,
    //         mSessionId = 0,
    //         mMatchmakingResult = MatchmakingResult.SESSION_CANCELED,
    //         mUserSessionId = 0
    //     });
    // });
    // return Task.FromResult(new StartMatchmakingResponse()
    // {
    //     mSessionId = 0,
    // });


    public override Task<NullStruct> FinalizeGameCreationAsync(UpdateGameSessionRequest request,
        BlazeRpcContext context)
    {
        NotifyGameSessionUpdatedAsync(context.BlazeConnection, new GameSessionUpdatedNotification
        {
            mGameId = request.mGameId,
            mXnetNonce = request.mXnetNonce,
            mXnetSession = request.mXnetSession,
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
        return Task.FromResult(new NullStruct());
    }

    // public override Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, BlazeRpcContext context)
    // {
    //     HockeyUser hockeyUser = Program.getHockeyUser(context.BlazeConnection);
    //     hockeyUser.NetworkAddress = request.mHostNetworkAddressList;
    //     uint gameID = gameIDCounter++;
    //     ReplicatedGameData gameData = new ReplicatedGameData
    //     {
    //         mAdminPlayerList = request.mAdminPlayerList,
    //         mGameAttribs = request.mGameAttribs,
    //         mSlotCapacities = request.mSlotCapacities,
    //         mEntryCriteriaMap = request.mEntryCriteriaMap,
    //         mGameId = gameID,
    //         mGameName = request.mGameName,
    //         mGameSettings = request.mGameSettings,
    //         mGameReportingId = 0,
    //         mGameState = GameState.PRE_GAME, //TODO FIDDLE
    //         mGameProtocolVersion = request.mGameProtocolVersion,
    //         mHostNetworkAddressList = request.mHostNetworkAddressList,
    //         mTopologyHostSessionId = 0, //TODO
    //         mIgnoreEntryCriteriaWithInvite = true,
    //         mMeshAttribs = request.mMeshAttribs,
    //         mMaxPlayerCapacity = request.mMaxPlayerCapacity,
    //         mNetworkQosData = default, //TODO
    //         mNetworkTopology = GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED, //TODO
    //         mPlatformHostInfo = default, //TODO
    //         mPingSiteAlias = request.mGamePingSiteAlias,
    //         mQueueCapacity = request.mQueueCapacity,
    //         mTopologyHostInfo = default, //TODO
    //         mUUID = gameID.ToString(),
    //         mVoipNetwork = VoipTopology.VOIP_DISABLED,
    //         mGameProtocolVersionString = request.mGameProtocolVersionString,
    //         mXnetNonce = new byte[]
    //         {
    //             //TODO
    //         },
    //         mXnetSession = new byte[]
    //         {
    //             //TODO
    //         }
    //     };
    //     List<ReplicatedGamePlayer> replicatedGamePlayers = new List<ReplicatedGamePlayer>
    //     {
    //         new ReplicatedGamePlayer
    //         {
    //             mCustomData = new byte[]
    //             {
    //             },
    //             mExternalId = hockeyUser.userId,
    //             mGameId = gameID,
    //             mAccountLocale = 1701729619,
    //             mPlayerName = hockeyUser.username,
    //             mNetworkQosData = default,
    //             mPlayerAttribs = request.mHostPlayerAttribs,
    //             mPlayerId = (uint)hockeyUser.userId,
    //             mNetworkAddress = hockeyUser.NetworkAddress,
    //             mSlotId = 1,
    //             mSlotType = request.mJoiningSlotType,
    //             mPlayerState = PlayerState.RESERVED,
    //             mPlayerSessionId = (uint)hockeyUser.userId, //TODO ????
    //         },
    //         // new ReplicatedGamePlayer
    //         // {
    //         //     mCustomData = new byte[]
    //         //     {
    //         //     },
    //         //     mExternalId = 1,
    //         //     mGameId = 0,
    //         //     mAccountLocale = 1701729619,
    //         //     mPlayerName = "Dummy",
    //         //     mNetworkQosData = default,
    //         //     mPlayerAttribs = request.mHostPlayerAttribs,
    //         //     mPlayerId = 1,
    //         //     mNetworkAddress = request.mHostNetworkAddressList,
    //         //     mSlotId = 1,
    //         //     mSlotType = request.mJoiningSlotType,
    //         //     mPlayerState = PlayerState.ACTIVE_CONNECTED,
    //         //     mTeamId = 1,
    //         //     mTeamIndex = 1,
    //         //     mJoinedGameTimestamp = 0,
    //         //     mPlayerSessionId = 1
    //         // }
    //     };
    //     Task.Run(async () =>
    //     {
    //         await Task.Delay(10);
    //         // NotifyGameCreatedAsync(context.BlazeConnection, new NotifyGameCreated
    //         // {
    //         //     mGameId = 0
    //         // });
    //         Task.Run(async () =>
    //         {
    //             await Task.Delay(1000);
    //             NotifyJoinGameAsync(context.BlazeConnection, new NotifyJoinGame
    //             {
    //                 mJoinErr = 0,
    //                 mGameData = gameData,
    //                 mMatchmakingSessionId = 0,
    //                 mGameRoster = replicatedGamePlayers,
    //             });
    //         });
    //     });
    //
    //
    //     return Task.FromResult(new CreateGameResponse
    //     {
    //         mGameData = gameData,
    //         mGameId = 0,
    //         mHostId = 0,
    //         mGameRoster = replicatedGamePlayers,
    //     });
    // }

    public static ReplicatedGameData RankedGameData(uint gameID, NetworkAddress host)
    {
        return new ReplicatedGameData
        {
            mGameAttribs = new SortedDictionary<string, string>()
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
                },
            },
            mSlotCapacities = new List<ushort>()
            {
                0, 2
            }, //TODO
            mEntryCriteriaMap = new SortedDictionary<string, string>(),
            mGameId = gameID,
            mGameName = "game" + gameID,
            mGameProtocolVersionHash = 0,
            mGameSettings = GameSettings.Ranked,
            mGameReportingId = 0,
            mGameState = GameState.NEW_STATE,
            mGameProtocolVersion = 1,
            mHostNetworkAddressList = host,
            mTopologyHostSessionId = 0,
            mIgnoreEntryCriteriaWithInvite = false,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mMaxPlayerCapacity = 2,
            mNetworkQosData = default,
            mNetworkTopology = GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED,
            mPersistedGameId = gameID.ToString(),
            mPersistedGameIdSecret = new byte[]
            {
            },
            mPlatformHostInfo = default,
            mPingSiteAlias = "qos",
            mQueueCapacity = 0,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = 0,
                mSlotId = 0
            },
            mUUID = "game" + gameID,
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
}
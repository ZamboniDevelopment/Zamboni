using System.Linq;
using System.Threading.Tasks;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;
using BlazeCommon;
using NLog;

namespace Zamboni.Components.Blaze;

public class GameManagerComponent : GameManagerBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static void Trigger()
    {
        if (Manager.QueuedMatchZamboniUsers.Count >= 2)
        {
            var hockeyUserA = Manager.QueuedMatchZamboniUsers[0];
            var hockeyUserB = Manager.QueuedMatchZamboniUsers[1];
            Manager.QueuedMatchZamboniUsers.Remove(hockeyUserA);
            Manager.QueuedMatchZamboniUsers.Remove(hockeyUserB);
            SendToRankedMatchGame(hockeyUserA, hockeyUserB, false);
        }

        if (Manager.QueuedShootoutZamboniUsers.Count >= 2)
        {
            var hockeyUserA = Manager.QueuedShootoutZamboniUsers[0];
            var hockeyUserB = Manager.QueuedShootoutZamboniUsers[1];
            Manager.QueuedShootoutZamboniUsers.Remove(hockeyUserA);
            Manager.QueuedShootoutZamboniUsers.Remove(hockeyUserB);
            SendToRankedMatchGame(hockeyUserA, hockeyUserB, true);
        }
    }

    private static void SendToRankedMatchGame(ZamboniUser host, ZamboniUser notHost, bool shootout)
    {
        var zamboniGame = new ZamboniGame(host, notHost, shootout);

        NotifyGameCreatedAsync(host.BlazeServerConnection, new NotifyGameCreated
        {
            mGameId = zamboniGame.GameId
        });

        NotifyGameCreatedAsync(notHost.BlazeServerConnection, new NotifyGameCreated
        {
            mGameId = zamboniGame.GameId
        });

        NotifyMatchmakingFinishedAsync(host.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = zamboniGame.GameId,
            mMaxPossibleFitScore = 10,
            mSessionId = (uint)host.UserId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
            mUserSessionId = (uint)host.UserId
        });
        NotifyMatchmakingFinishedAsync(notHost.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = zamboniGame.GameId,
            mMaxPossibleFitScore = 10,
            mSessionId = (uint)notHost.UserId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_NEW_GAME,
            mUserSessionId = (uint)notHost.UserId
        });


        //This is not really a right solution, but works somehow for now...

        Task.Run(async () =>
        {
            await Task.Delay(10);

            await NotifyJoinGameAsync(host.BlazeServerConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = zamboniGame.ReplicatedGameData,
                mMatchmakingSessionId = (uint)host.UserId,
                mGameRoster = zamboniGame.ReplicatedGamePlayers
            });

            await NotifyJoinGameAsync(notHost.BlazeServerConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = zamboniGame.ReplicatedGameData,
                mMatchmakingSessionId = (uint)notHost.UserId,
                mGameRoster = zamboniGame.ReplicatedGamePlayers
            });
        });
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        var zamboniUser = Manager.GetZamboniUser(context.BlazeConnection);
        foreach (var loopUser in Manager.QueuedMatchZamboniUsers.ToList().Where(loopUser => loopUser.UserId.Equals(zamboniUser.UserId))) Manager.QueuedMatchZamboniUsers.Remove(loopUser);
        foreach (var loopUser in Manager.QueuedShootoutZamboniUsers.ToList().Where(loopUser => loopUser.UserId.Equals(zamboniUser.UserId))) Manager.QueuedShootoutZamboniUsers.Remove(loopUser);

        Logger.Info(zamboniUser.Username + " queued");
        //Maybe we should parse all criterias...
        var gameMode = request.mCriteriaData.mGenericRulePrefsList.Find(prefs => prefs.mRuleName.Equals("OSDK_gameMode")).mDesiredValues[0];
        switch (gameMode)
        {
            case "1":
                Manager.QueuedMatchZamboniUsers.Add(zamboniUser);
                break;
            case "2":
                Manager.QueuedShootoutZamboniUsers.Add(zamboniUser);
                break;
            default:
                Logger.Warn("Unknown game mode: " + gameMode);
                break;
        }

        Task.Run(async () =>
        {
            await Task.Delay(100);
            Trigger();
        });
        return Task.FromResult(new StartMatchmakingResponse
        {
            mSessionId = (uint)zamboniUser.UserId
        });
    }


    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request, BlazeRpcContext context)
    {
        var zamboniUser = Manager.GetZamboniUser(context.BlazeConnection);
        Manager.QueuedMatchZamboniUsers.Remove(zamboniUser);
        Manager.QueuedShootoutZamboniUsers.Remove(zamboniUser);
        Logger.Info(zamboniUser.Username + " unqueued");
        return Task.FromResult(new NullStruct());
    }

    public override Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, BlazeRpcContext context)
    {
        var host = Manager.GetZamboniUser(context.BlazeConnection);

        var zamboniGame = new ZamboniGame(host, request);
        Task.Run(async () =>
        {
            await Task.Delay(100);
            zamboniGame.AddGameParticipant(host);
        });

        return Task.FromResult(new CreateGameResponse
        {
            mGameData = zamboniGame.ReplicatedGameData,
            mGameId = zamboniGame.GameId,
            mHostId = (uint)host.UserId,
            mGameRoster = zamboniGame.ReplicatedGamePlayers
        });
    }

    public override Task<JoinGameResponse> JoinGameAsync(JoinGameRequest request, BlazeRpcContext context)
    {
        var accepter = Manager.GetZamboniUser(context.BlazeConnection);
        var game = Manager.GetZamboniGame(request.mGameId);

        Task.Run(async () =>
        {
            await Task.Delay(100);
            game.AddGameParticipant(accepter);
        });


        return Task.FromResult(new JoinGameResponse
        {
            mGameId = request.mGameId
        });
    }

    public override Task<NullStruct> RemovePlayerAsync(RemovePlayerRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        var zamboniUser = Manager.GetZamboniUser(request.mPlayerId);

        if (zamboniGame == null || zamboniUser == null) return Task.FromResult(new NullStruct());

        zamboniGame.RemoveGameParticipant(zamboniUser);
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateGameSessionAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        if (zamboniGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in zamboniGame.ZamboniUsers)
            NotifyGameSessionUpdatedAsync(zamboniUser.BlazeServerConnection, new GameSessionUpdatedNotification
            {
                mGameId = request.mGameId,
                mXnetNonce = request.mXnetNonce,
                mXnetSession = request.mXnetSession
            });
        return Task.FromResult(new NullStruct());
    }


    public override Task<NullStruct> FinalizeGameCreationAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        if (zamboniGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in zamboniGame.ZamboniUsers)
            NotifyGameSessionUpdatedAsync(zamboniUser.BlazeServerConnection, new GameSessionUpdatedNotification
            {
                mGameId = request.mGameId,
                mXnetNonce = request.mXnetNonce,
                mXnetSession = request.mXnetSession
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> AdvanceGameStateAsync(AdvanceGameStateRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        if (zamboniGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mGameState = request.mNewGameState;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in zamboniGame.ZamboniUsers)
            NotifyGameStateChangeAsync(zamboniUser.BlazeServerConnection, new NotifyGameStateChange
            {
                mGameId = request.mGameId,
                mNewGameState = request.mNewGameState
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> SetGameSettingsAsync(SetGameSettingsRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        if (zamboniGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mGameSettings = request.mGameSettings;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in zamboniGame.ZamboniUsers)
            NotifyGameSettingsChangeAsync(zamboniUser.BlazeServerConnection, new NotifyGameSettingsChange
            {
                mGameSettings = request.mGameSettings,
                mGameId = request.mGameId
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateMeshConnectionAsync(UpdateMeshConnectionRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        if (zamboniGame == null) return Task.FromResult(new NullStruct());

        foreach (var playerConnectionStatus in request.mMeshConnectionStatusList)
            switch (playerConnectionStatus.mPlayerNetConnectionStatus)
            {
                case PlayerNetConnectionStatus.CONNECTED:
                {
                    var statePacket = new NotifyGamePlayerStateChange
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer,
                        mPlayerState = PlayerState.ACTIVE_CONNECTED
                    };
                    zamboniGame.NotifyParticipants(statePacket);

                    var joinCompletedPacket = new NotifyPlayerJoinCompleted
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer
                    };
                    zamboniGame.NotifyParticipants(joinCompletedPacket);
                    break;
                }
                case PlayerNetConnectionStatus.ESTABLISHING_CONNECTION:
                {
                    var statePacket = new NotifyGamePlayerStateChange
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer,
                        mPlayerState = PlayerState.ACTIVE_CONNECTING
                    };
                    zamboniGame.NotifyParticipants(statePacket);
                    break;
                }
                case PlayerNetConnectionStatus.DISCONNECTED:
                {
                    var zamboniUser = Manager.GetZamboniUser(playerConnectionStatus.mTargetPlayer);
                    if (zamboniGame.ZamboniUsers.Contains(zamboniUser))
                    {
                        Logger.Warn("Zamboniuser Existed in game object");
                        zamboniGame.RemoveGameParticipant(zamboniUser);
                        break;
                    }

                    Logger.Warn("Zamboniuser Didnt exist in game object");
                    var leavePacket = new NotifyPlayerRemoved
                    {
                        mPlayerRemovedTitleContext = 0, //What is this?
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer,
                        mPlayerRemovedReason = PlayerRemovedReason.PLAYER_CONN_LOST //General leave message?
                    };
                    zamboniGame.NotifyParticipants(leavePacket);
                    break;
                }
            }

        return Task.FromResult(new NullStruct());
    }
}
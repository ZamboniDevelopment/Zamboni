using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;
using BlazeCommon;
using NLog;

namespace Zamboni.Components.Blaze;

public class GameManagerComponent : GameManagerBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly Timer Timer;

    static GameManagerComponent()
    {
        Timer = new Timer(2000);
        Timer.Elapsed += OnTimedEvent;
        Timer.AutoReset = true;
        Timer.Enabled = true;
    }

    private static void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        if (Manager.QueuedUsers.Count <= 1) return;

        var grouped = Manager.QueuedUsers.GroupBy(u => u.StartMatchmakingRequest.mCriteriaData.mGenericRulePrefsList.Find(prefs => prefs.mRuleName.Equals("OSDK_gameMode")).mDesiredValues[0]);

        foreach (var group in grouped)
        {
            var users = group.ToList();

            while (users.Count >= 2)
            {
                var queuedUserA = users[0];
                var queuedUserB = users[1];

                users.RemoveRange(0, 2);
                Manager.QueuedUsers.Remove(queuedUserA);
                Manager.QueuedUsers.Remove(queuedUserB);

                SendToMatchMakingGame(queuedUserA, queuedUserB, queuedUserA.StartMatchmakingRequest, group.Key);
            }
        }
    }

    private static void SendToMatchMakingGame(QueuedUser host, QueuedUser notHost, StartMatchmakingRequest startMatchmakingRequest, string gameMode)
    {
        var zamboniGame = new ZamboniGame(host.ZamboniUser, startMatchmakingRequest, gameMode);

        zamboniGame.AddGameParticipant(host.ZamboniUser, host.MatchmakingSessionId);
        zamboniGame.AddGameParticipant(notHost.ZamboniUser, notHost.MatchmakingSessionId);

        NotifyMatchmakingFinishedAsync(host.ZamboniUser.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = zamboniGame.GameId,
            mMaxPossibleFitScore = 10,
            mSessionId = host.MatchmakingSessionId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_EXISTING_GAME,
            mUserSessionId = (uint)host.ZamboniUser.UserId
        });

        NotifyMatchmakingFinishedAsync(notHost.ZamboniUser.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = zamboniGame.GameId,
            mMaxPossibleFitScore = 10,
            mSessionId = notHost.MatchmakingSessionId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_EXISTING_GAME,
            mUserSessionId = (uint)notHost.ZamboniUser.UserId
        });
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        var zamboniUser = Manager.GetZamboniUser(context.BlazeConnection);

        var queuedUser = new QueuedUser(zamboniUser, request);
        Manager.QueuedUsers.Add(queuedUser);

        return Task.FromResult(new StartMatchmakingResponse
        {
            mSessionId = queuedUser.MatchmakingSessionId
        });
    }

    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request, BlazeRpcContext context)
    {
        var zamboniUser = Manager.GetZamboniUser(context.BlazeConnection);
        var queuedUser = Manager.GetQueuedUser(zamboniUser);
        if (queuedUser != null) Manager.QueuedUsers.Remove(queuedUser);
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

    //Yes, this is the request that client sends when he wants to create an OTP Lobby. Real ea type shi
    // STILL WIP
    // public override Task<JoinGameResponse> ResetDedicatedServerAsync(CreateGameRequest request, BlazeRpcContext context)
    // {
    //     var host = Manager.GetZamboniUser(context.BlazeConnection);
    //
    //     var zamboniGame = new ZamboniGame(host, request);
    //     Task.Run(async () =>
    //     {
    //         await Task.Delay(100);
    //         zamboniGame.AddGameParticipant(host);
    //     });
    //
    //     return Task.FromResult(new JoinGameResponse
    //     {
    //         mGameId = zamboniGame.GameId
    //     });
    // }

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
                    zamboniGame.RemoveGameParticipant(zamboniUser);
                    break;
                }
                default:
                    Logger.Debug("Unknown player connection status: " + playerConnectionStatus.mPlayerNetConnectionStatus);
                    break;
            }

        return Task.FromResult(new NullStruct());
    }
}
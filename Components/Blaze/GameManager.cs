using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;
using BlazeCommon;
using NLog;

namespace Zamboni.Components.Blaze;

public class GameManager : GameManagerBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly Timer Timer;

    static GameManager()
    {
        Timer = new Timer(2000);
        Timer.Elapsed += OnTimedEvent;
        Timer.AutoReset = true;
        Timer.Enabled = true;
    }

    private static void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        ServerManager.GetServerGames().RemoveAll(game => game.ServerPlayers.Count == 0); // How to not fix bugs
        
        if (ServerManager.GetQueuedPlayers().Count <= 1) return;

        var grouped = ServerManager.GetQueuedPlayers().GroupBy(u => u.StartMatchmakingRequest.mCriteriaData.mGenericRulePrefsList.Find(prefs => prefs.mRuleName.Equals("OSDK_gameMode")).mDesiredValues[0]);

        foreach (var group in grouped)
        {
            var players = group.ToList();

            while (players.Count >= 2)
            {
                var queuedPlayerA = players[0];
                var queuedPlayerB = players[1];

                players.RemoveRange(0, 2);
                ServerManager.RemoveQueuedPlayer(queuedPlayerA);
                ServerManager.RemoveQueuedPlayer(queuedPlayerB);

                SendToMatchMakingGame(queuedPlayerA, queuedPlayerB, queuedPlayerA.StartMatchmakingRequest, group.Key);
            }
        }
    }

    private static void SendToMatchMakingGame(QueuedPlayer host, QueuedPlayer notHost, StartMatchmakingRequest startMatchmakingRequest, string gameMode)
    {
        var serverGame = new ServerGame(host.ServerPlayer, startMatchmakingRequest, gameMode);

        serverGame.AddGameParticipant(host.ServerPlayer, host.MatchmakingSessionId);
        serverGame.AddGameParticipant(notHost.ServerPlayer, notHost.MatchmakingSessionId);

        NotifyMatchmakingFinishedAsync(host.ServerPlayer.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = serverGame.ReplicatedGameData.mGameId,
            mMaxPossibleFitScore = 10,
            mSessionId = host.MatchmakingSessionId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_EXISTING_GAME,
            mUserSessionId = host.ServerPlayer.UserIdentification.mBlazeId
        });

        NotifyMatchmakingFinishedAsync(notHost.ServerPlayer.BlazeServerConnection, new NotifyMatchmakingFinished
        {
            mFitScore = 10,
            mGameId = serverGame.ReplicatedGameData.mGameId,
            mMaxPossibleFitScore = 10,
            mSessionId = notHost.MatchmakingSessionId,
            mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_EXISTING_GAME,
            mUserSessionId = notHost.ServerPlayer.UserIdentification.mBlazeId
        });
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        var serverPlayer = ServerManager.GetServerPlayer(context.BlazeConnection);

        var queuedPlayer = new QueuedPlayer(serverPlayer, request);
        ServerManager.AddQueuedPlayer(queuedPlayer);

        return Task.FromResult(new StartMatchmakingResponse
        {
            mSessionId = queuedPlayer.MatchmakingSessionId
        });
    }

    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request, BlazeRpcContext context)
    {
        var serverPlayer = ServerManager.GetServerPlayer(context.BlazeConnection);
        var queuedPlayer = ServerManager.GetQueuedPlayer(serverPlayer);
        if (queuedPlayer != null) ServerManager.RemoveQueuedPlayer(queuedPlayer);
        return Task.FromResult(new NullStruct());
    }

    public override Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, BlazeRpcContext context)
    {
        var host = ServerManager.GetServerPlayer(context.BlazeConnection);

        var serverGame = new ServerGame(host, request);
        Task.Run(async () =>
        {
            await Task.Delay(100);
            serverGame.AddGameParticipant(host);
        });

        return Task.FromResult(new CreateGameResponse
        {
            mGameData = serverGame.ReplicatedGameData,
            mGameId = serverGame.ReplicatedGameData.mGameId,
            mHostId = host.UserIdentification.mBlazeId,
            mGameRoster = serverGame.ReplicatedGamePlayers
        });
    }

    // Yes, this is the request that client sends when he wants to create an OTP Lobby. Real ea type shi
    //  STILL WIP
    public override Task<JoinGameResponse> ResetDedicatedServerAsync(CreateGameRequest request, BlazeRpcContext context)
    {
        var host = ServerManager.GetServerPlayer(context.BlazeConnection);

        var serverGame = new ServerGame(host, request);
        Task.Run(async () =>
        {
            await Task.Delay(100);
            serverGame.AddGameParticipant(host);
        });

        return Task.FromResult(new JoinGameResponse
        {
            mGameId = serverGame.ReplicatedGameData.mGameId
        });
    }

    public override Task<JoinGameResponse> JoinGameAsync(JoinGameRequest request, BlazeRpcContext context)
    {
        var accepter = ServerManager.GetServerPlayer(context.BlazeConnection);
        var game = ServerManager.GetServerGame(request.mGameId);

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
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        var serverPlayer = ServerManager.GetServerPlayer(request.mPlayerId);

        if (serverGame == null || serverPlayer == null) return Task.FromResult(new NullStruct());

        serverGame.RemoveGameParticipant(serverPlayer);
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateGameSessionAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers)
            NotifyGameSessionUpdatedAsync(serverPlayer.BlazeServerConnection, new GameSessionUpdatedNotification
            {
                mGameId = request.mGameId,
                mXnetNonce = request.mXnetNonce,
                mXnetSession = request.mXnetSession
            });
        return Task.FromResult(new NullStruct());
    }


    public override Task<NullStruct> FinalizeGameCreationAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers)
            NotifyGameSessionUpdatedAsync(serverPlayer.BlazeServerConnection, new GameSessionUpdatedNotification
            {
                mGameId = request.mGameId,
                mXnetNonce = request.mXnetNonce,
                mXnetSession = request.mXnetSession
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> AdvanceGameStateAsync(AdvanceGameStateRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mGameState = request.mNewGameState;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers)
            NotifyGameStateChangeAsync(serverPlayer.BlazeServerConnection, new NotifyGameStateChange
            {
                mGameId = request.mGameId,
                mNewGameState = request.mNewGameState
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> SetGameSettingsAsync(SetGameSettingsRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mGameSettings = request.mGameSettings;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers)
            NotifyGameSettingsChangeAsync(serverPlayer.BlazeServerConnection, new NotifyGameSettingsChange
            {
                mGameSettings = request.mGameSettings,
                mGameId = request.mGameId
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateMeshConnectionAsync(UpdateMeshConnectionRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

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
                    serverGame.NotifyParticipants(statePacket);

                    var joinCompletedPacket = new NotifyPlayerJoinCompleted
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer
                    };
                    serverGame.NotifyParticipants(joinCompletedPacket);
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
                    serverGame.NotifyParticipants(statePacket);
                    break;
                }
                case PlayerNetConnectionStatus.DISCONNECTED:
                {
                    var serverPlayer = ServerManager.GetServerPlayer(playerConnectionStatus.mTargetPlayer);
                    serverGame.RemoveGameParticipant(serverPlayer);
                    break;
                }
                default:
                    Logger.Debug("Unknown player connection status: " + playerConnectionStatus.mPlayerNetConnectionStatus);
                    break;
            }

        return Task.FromResult(new NullStruct());
    }
}
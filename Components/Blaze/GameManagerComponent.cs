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
        if (Manager.ZamboniGames.Capacity == 0)
        {
            var hockeyUserA = Manager.QueuedZamboniUsers[0];

            var zamboniGame = new ZamboniGame(hockeyUserA);
            Manager.ZamboniGames.Add(zamboniGame);

            NotifyMatchmakingFinishedAsync(hockeyUserA.BlazeServerConnection, new NotifyMatchmakingFinished
            {
                mFitScore = 10,
                mGameId = zamboniGame.GameId,
                mMaxPossibleFitScore = 10,
                mSessionId = (uint)hockeyUserA.UserId,
                mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
                mUserSessionId = (uint)hockeyUserA.UserId
            });

            NotifyGameCreatedAsync(hockeyUserA.BlazeServerConnection, new NotifyGameCreated
            {
                mGameId = zamboniGame.GameId
            });

            NotifyJoinGameAsync(hockeyUserA.BlazeServerConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = zamboniGame.ReplicatedGameData,
                mMatchmakingSessionId = (uint)hockeyUserA.UserId,
                mGameRoster = zamboniGame.ReplicatedGamePlayers
            });

            Manager.QueuedZamboniUsers.Remove(hockeyUserA);
        }
        else
        {
            var hockeyUserB = Manager.QueuedZamboniUsers[0];

            var zamboniGame = Manager.ZamboniGames[0];

            NotifyGameCreatedAsync(hockeyUserB.BlazeServerConnection, new NotifyGameCreated
            {
                mGameId = zamboniGame.GameId
            });

            NotifyMatchmakingFinishedAsync(hockeyUserB.BlazeServerConnection, new NotifyMatchmakingFinished
            {
                mFitScore = 10,
                mGameId = zamboniGame.GameId,
                mMaxPossibleFitScore = 10,
                mSessionId = (uint)hockeyUserB.UserId,
                mMatchmakingResult = MatchmakingResult.SUCCESS_JOINED_NEW_GAME,
                mUserSessionId = (uint)hockeyUserB.UserId
            });


            var replicatedGamePlayerB = hockeyUserB.ToReplicatedGamePlayer(1, zamboniGame.GameId);

            zamboniGame.ReplicatedGamePlayers.Add(replicatedGamePlayerB);
            NotifyJoinGameAsync(hockeyUserB.BlazeServerConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = zamboniGame.ReplicatedGameData,
                mMatchmakingSessionId = (uint)hockeyUserB.UserId,
                mGameRoster = zamboniGame.ReplicatedGamePlayers
            });
            NotifyPlayerJoiningAsync(zamboniGame.ZamboniUsers[0].BlazeServerConnection, new NotifyPlayerJoining
            {
                mGameId = zamboniGame.GameId,
                mJoiningPlayer = replicatedGamePlayerB
            });
            NotifyPlayerJoiningAsync(zamboniGame.ZamboniUsers[1].BlazeServerConnection, new NotifyPlayerJoining
            {
                mGameId = zamboniGame.GameId,
                mJoiningPlayer = replicatedGamePlayerB
            });


            Manager.QueuedZamboniUsers.Remove(hockeyUserB);
        }
    }

    public override Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, BlazeRpcContext context)
    {
        var zamboniUser = Manager.GetZamboniUser(context.BlazeConnection);
        var zamboniGame = new ZamboniGame(zamboniUser);
        Manager.ZamboniGames.Add(zamboniGame);

        Task.Run(async () =>
        {
            await Task.Delay(100);
            NotifyGameCreatedAsync(context.BlazeConnection, new NotifyGameCreated
            {
                mGameId = 0
            });
            NotifyJoinGameAsync(context.BlazeConnection, new NotifyJoinGame
            {
                mJoinErr = 0,
                mGameData = zamboniGame.ReplicatedGameData,
                // mMatchmakingSessionId = 0,
                mGameRoster = zamboniGame.ReplicatedGamePlayers
            });
        });


        return Task.FromResult(new CreateGameResponse
        {
            mGameData = zamboniGame.ReplicatedGameData,
            mGameId = zamboniGame.GameId,
            mHostId = (uint)zamboniUser.UserId,
            mGameRoster = zamboniGame.ReplicatedGamePlayers
        });
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        var hockeyUser = Manager.GetZamboniUser(context.BlazeConnection);
        Logger.Warn(hockeyUser.Username + " queued");
        Manager.QueuedZamboniUsers.Add(hockeyUser);

        Task.Run(async () =>
        {
            await Task.Delay(100);
            Trigger();
        });
        return Task.FromResult(new StartMatchmakingResponse
        {
            mSessionId = (uint)hockeyUser.UserId
        });
    }


    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request, BlazeRpcContext context)
    {
        var hockeyUser = Manager.GetZamboniUser(context.BlazeConnection);
        Manager.QueuedZamboniUsers.Remove(hockeyUser);
        Logger.Warn(hockeyUser.Username + " unqueued");
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateGameSessionAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in Manager.ZamboniUsers)
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

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in Manager.ZamboniUsers)
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

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mGameState = request.mNewGameState;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in Manager.ZamboniUsers)
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

        var replicatedGameData = zamboniGame.ReplicatedGameData;
        replicatedGameData.mGameSettings = request.mGameSettings;

        zamboniGame.ReplicatedGameData = replicatedGameData;

        foreach (var zamboniUser in Manager.ZamboniUsers)
            NotifyGameSettingsChangeAsync(zamboniUser.BlazeServerConnection, new NotifyGameSettingsChange
            {
                mGameSettings = request.mGameSettings,
                mGameId = request.mGameId
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateMeshConnectionAsync(UpdateMeshConnectionRequest request, BlazeRpcContext context)
    {
        foreach (var zamboniUser in Manager.ZamboniUsers)
            NotifyGamePlayerStateChangeAsync(zamboniUser.BlazeServerConnection, new NotifyGamePlayerStateChange
            {
                mGameId = request.mGameId,
                mPlayerId = request.mMeshConnectionStatusList[0].mTargetPlayer,
                mPlayerState = PlayerState.ACTIVE_CONNECTED
            });

        foreach (var zamboniUser in Manager.ZamboniUsers)
            NotifyPlayerJoinCompletedAsync(zamboniUser.BlazeServerConnection, new NotifyPlayerJoinCompleted
            {
                mGameId = request.mGameId,
                mPlayerId = request.mMeshConnectionStatusList[0].mTargetPlayer
            });
        return Task.FromResult(new NullStruct());
    }
}
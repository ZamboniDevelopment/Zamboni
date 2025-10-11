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
        if (Manager.QueuedZamboniUsers.Capacity < 2) return;
        var hockeyUserA = Manager.QueuedZamboniUsers[0];
        var hockeyUserB = Manager.QueuedZamboniUsers[1];
        Manager.QueuedZamboniUsers.Remove(hockeyUserA);
        Manager.QueuedZamboniUsers.Remove(hockeyUserB);
        SendToGame(hockeyUserA, hockeyUserB);
    }

    private static void SendToGame(ZamboniUser host, ZamboniUser notHost)
    {
        var zamboniGame = new ZamboniGame(host, notHost);
        Manager.ZamboniGames.Add(zamboniGame);

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
        foreach (var loopUser in Manager.QueuedZamboniUsers.ToList().Where(loopUser => loopUser.UserId.Equals(zamboniUser.UserId))) Manager.QueuedZamboniUsers.Remove(loopUser);

        Logger.Info(zamboniUser.Username + " queued");
        Manager.QueuedZamboniUsers.Add(zamboniUser);

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
        Manager.QueuedZamboniUsers.Remove(zamboniUser);
        Logger.Info(zamboniUser.Username + " unqueued");
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> RemovePlayerAsync(RemovePlayerRequest request, BlazeRpcContext context)
    {
        var zamboniGame = Manager.GetZamboniGame(request.mGameId);
        if (zamboniGame == null) return Task.FromResult(new NullStruct());

        foreach (var zamboniUser in zamboniGame.ZamboniUsers)
        {
            NotifyPlayerRemovedAsync(zamboniUser.BlazeServerConnection, new NotifyPlayerRemoved
            {
                mPlayerRemovedTitleContext = 0,
                mGameId = request.mGameId,
                mPlayerId = (uint)zamboniGame.ZamboniUsers[0].UserId,
                mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
            });
            NotifyPlayerRemovedAsync(zamboniUser.BlazeServerConnection, new NotifyPlayerRemoved
            {
                mPlayerRemovedTitleContext = 0,
                mGameId = request.mGameId,
                mPlayerId = (uint)zamboniGame.ZamboniUsers[1].UserId,
                mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
            });
        }

        Manager.ZamboniGames.Remove(zamboniGame);

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

        var playerConnectionStatus = request.mMeshConnectionStatusList[0];

        if (playerConnectionStatus.mPlayerNetConnectionStatus.Equals(PlayerNetConnectionStatus.CONNECTED))
        {
            foreach (var zamboniUser in zamboniGame.ZamboniUsers)
                NotifyGamePlayerStateChangeAsync(zamboniUser.BlazeServerConnection, new NotifyGamePlayerStateChange
                {
                    mGameId = request.mGameId,
                    mPlayerId = playerConnectionStatus.mTargetPlayer,
                    mPlayerState = PlayerState.ACTIVE_CONNECTED
                });

            foreach (var zamboniUser in zamboniGame.ZamboniUsers)
                NotifyPlayerJoinCompletedAsync(zamboniUser.BlazeServerConnection, new NotifyPlayerJoinCompleted
                {
                    mGameId = request.mGameId,
                    mPlayerId = playerConnectionStatus.mTargetPlayer
                });
        }
        else
        {
            foreach (var zamboniUser in zamboniGame.ZamboniUsers)
            {
                NotifyPlayerRemovedAsync(zamboniUser.BlazeServerConnection, new NotifyPlayerRemoved
                {
                    mPlayerRemovedTitleContext = 0,
                    mGameId = request.mGameId,
                    mPlayerId = (uint)zamboniGame.ZamboniUsers[0].UserId,
                    mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
                });
                NotifyPlayerRemovedAsync(zamboniUser.BlazeServerConnection, new NotifyPlayerRemoved
                {
                    mPlayerRemovedTitleContext = 0,
                    mGameId = request.mGameId,
                    mPlayerId = (uint)zamboniGame.ZamboniUsers[1].UserId,
                    mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
                });
            }

            Manager.ZamboniGames.Remove(zamboniGame);
        }


        return Task.FromResult(new NullStruct());
    }
}
using System.Collections.Generic;
using System.Linq;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;

namespace Zamboni;

public class ServerGame
{
    public ServerGame(ServerPlayer host, CreateGameRequest request)
    {
        var gameId = Program.Database.GetNextGameId();
        ReplicatedGameData = new ReplicatedGameData
        {
            mAdminPlayerList = new List<uint>
            {
                host.UserIdentification.mBlazeId
            },
            mGameAttribs = request.mGameAttribs,
            mSlotCapacities = request.mSlotCapacities,
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameId = gameId,
            mGameName = "game" + gameId,
            mGameSettings = request.mGameSettings,
            mGameReportingId = gameId,
            mGameState = GameState.INITIALIZING,
            mGameProtocolVersion = request.mGameProtocolVersion,
            mHostNetworkAddress = request.mHostNetworkAddress,
            mTopologyHostSessionId = host.UserIdentification.mBlazeId,
            mIgnoreEntryCriteriaWithInvite = request.mIgnoreEntryCriteriaWithInvite,
            mMeshAttribs = request.mMeshAttribs,
            mMaxPlayerCapacity = request.mMaxPlayerCapacity,
            mNetworkQosData = host.ExtendedData.mQosData,
            mNetworkTopology = request.mNetworkTopology,
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mBlazeId,
                mSlotId = 0
            },
            mPingSiteAlias = "qos",
            mQueueCapacity = request.mQueueCapacity,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mBlazeId,
                mSlotId = 0
            },
            mUUID = "game" + gameId,
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            }
        };
        ServerManager.AddServerGame(this);
    }

    public ServerGame(ServerPlayer host, StartMatchmakingRequest request, string gameMode)
    {
        var gameId = Program.Database.GetNextGameId();
        SortedDictionary<string, string> mGameAttribs;
        switch (gameMode)
        {
            case "1":
                mGameAttribs = VsGameAttribs();
                break;
            case "2":
                mGameAttribs = SoGameAttribs();
                break;
            case "3":
                mGameAttribs = OtpGameAttribs();
                break;
            default:
                return;
        }

        ReplicatedGameData = new ReplicatedGameData
        {
            mAdminPlayerList = new List<uint>
            {
                host.UserIdentification.mBlazeId
            },
            mGameAttribs = mGameAttribs,
            mSlotCapacities = Capacities(gameMode),
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameId = gameId,
            mGameName = "game" + gameId,
            mGameSettings = request.mGameSettings,
            mGameReportingId = gameId,
            mGameState = GameState.INITIALIZING,
            mGameProtocolVersion = 1,
            mHostNetworkAddress = host.ExtendedData.mAddress,
            mTopologyHostSessionId = host.UserIdentification.mBlazeId,
            mIgnoreEntryCriteriaWithInvite = request.mIgnoreEntryCriteriaWithInvite,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mMaxPlayerCapacity = Capacities(gameMode)[1],
            mNetworkQosData = host.ExtendedData.mQosData,
            mNetworkTopology = request.mNetworkTopology,
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mBlazeId,
                mSlotId = 0
            },
            mPingSiteAlias = "qos",
            mQueueCapacity = 0,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mBlazeId,
                mSlotId = 0
            },
            mUUID = "game" + gameId,
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            }
        };
        ServerManager.AddServerGame(this);
    }

    public List<ServerPlayer> ServerPlayers { get; } = new();
    public ReplicatedGameData ReplicatedGameData { get; set; }
    public List<ReplicatedGamePlayer> ReplicatedGamePlayers { get; set; } = new();

    private List<ushort> Capacities(string gameMode)
    {
        if (gameMode.Equals("3"))
            return new List<ushort>
            {
                0, 12
            };

        return new List<ushort>
        {
            0, 2
        };
    }

    public void AddGameParticipant(ServerPlayer serverPlayer, uint matchmakingSessionId = 0)
    {
        //TODO Lobby capacities?
        ServerPlayers.Add(serverPlayer);
        var replicatedGamePlayer = serverPlayer.ToReplicatedGamePlayer((byte)(ServerPlayers.Count - 1), ReplicatedGameData.mGameId);
        ReplicatedGamePlayers.Add(replicatedGamePlayer);

        GameManagerBase.Server.NotifyJoinGameAsync(serverPlayer.BlazeServerConnection, new NotifyJoinGame
        {
            mJoinErr = 0,
            mGameData = ReplicatedGameData,
            mMatchmakingSessionId = matchmakingSessionId,
            mGameRoster = ReplicatedGamePlayers
        });
        NotifyParticipants(new NotifyPlayerJoining
        {
            mGameId = ReplicatedGameData.mGameId,
            mJoiningPlayer = replicatedGamePlayer
        });
    }

    public void RemoveGameParticipant(ServerPlayer serverPlayer)
    {
        ServerPlayers.Remove(serverPlayer);
        ReplicatedGamePlayers.Remove(ReplicatedGamePlayers.Find(replicatedPlayer => replicatedPlayer.mPlayerId.Equals(serverPlayer.UserIdentification.mBlazeId)));
        NotifyParticipants(new NotifyPlayerRemoved
        {
            mPlayerRemovedTitleContext = 0, //??
            mGameId = ReplicatedGameData.mGameId,
            mPlayerId = serverPlayer.UserIdentification.mBlazeId,
            mPlayerRemovedReason = PlayerRemovedReason.PLAYER_LEFT
        });
        if (ServerPlayers.Count > 1) return;
        NotifyParticipants(new NotifyPlayerRemoved
        {
            mPlayerRemovedTitleContext = 0, //??
            mGameId = ReplicatedGameData.mGameId,
            mPlayerId = ServerPlayers[0].UserIdentification.mBlazeId,
            mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
        });
        ServerManager.RemoveServerGame(this);
    }

    public void NotifyParticipants(NotifyGamePlayerStateChange playerStateChange)
    {
        foreach (var serverPlayer in ServerPlayers) GameManagerBase.Server.NotifyGamePlayerStateChangeAsync(serverPlayer.BlazeServerConnection, playerStateChange);
    }

    public void NotifyParticipants(NotifyPlayerJoinCompleted playerJoinCompleted)
    {
        foreach (var serverPlayer in ServerPlayers) GameManagerBase.Server.NotifyPlayerJoinCompletedAsync(serverPlayer.BlazeServerConnection, playerJoinCompleted);
    }

    public void NotifyParticipants(NotifyPlayerRemoved playerRemoved)
    {
        foreach (var serverPlayer in ServerPlayers) GameManagerBase.Server.NotifyPlayerRemovedAsync(serverPlayer.BlazeServerConnection, playerRemoved);
    }

    private void NotifyParticipants(NotifyPlayerJoining playerJoining)
    {
        foreach (var serverPlayer in ServerPlayers) GameManagerBase.Server.NotifyPlayerJoiningAsync(serverPlayer.BlazeServerConnection, playerJoining);
    }

    private SortedDictionary<string, string> VsGameAttribs()
    {
        return new SortedDictionary<string, string>
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
        };
    }

    private SortedDictionary<string, string> OtpGameAttribs()
    {
        var vsGameAttribs = new SortedDictionary<string, string>(VsGameAttribs());
        vsGameAttribs.Add("ClubRules", "0");
        vsGameAttribs.Remove("CreatedPlays");
        vsGameAttribs.Remove("OSDK_gameMode");
        vsGameAttribs.Add("OSDK_gameMode", "3");
        return vsGameAttribs;
    }

    private SortedDictionary<string, string> SoGameAttribs()
    {
        return new SortedDictionary<string, string>
        {
            {
                "ShootoutSkillMatchup", "0"
            },
            {
                "ShootoutShotsPerRound", "5"
            },
            {
                "ShootoutRules", "1"
            },
            {
                "ShootoutGoalieControl", "1"
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
                "OSDK_gameMode", "2"
            }
        };
    }

    public override string ToString()
    {
        return "Players: " +
               string.Join(", ", ServerPlayers.Select(serverPlayer => serverPlayer.UserIdentification.mName)) +
               " gameId:" + ReplicatedGameData.mGameId +
               " state: " + ReplicatedGameData.mGameState +
               " type (1 vs game 2 shootout, 3 otp): " + ReplicatedGameData.mGameAttribs["OSDK_gameMode"];
    }
}
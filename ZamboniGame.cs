using System.Collections.Generic;
using System.Linq;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;
using Blaze2SDK.Components;

namespace Zamboni;

public class ZamboniGame
{
    public ZamboniGame(ZamboniUser host, CreateGameRequest createGameRequest)
    {
        GameId = Program.Database.GetNextGameId();
        ReplicatedGameData = new ReplicatedGameData
        {
            mAdminPlayerList = new List<uint>
            {
                (uint)host.UserId
            },
            mGameAttribs = createGameRequest.mGameAttribs,
            mSlotCapacities = createGameRequest.mSlotCapacities,
            mEntryCriteriaMap = createGameRequest.mEntryCriteriaMap,
            mGameId = GameId,
            mGameName = "game" + GameId,
            mGameSettings = createGameRequest.mGameSettings,
            mGameReportingId = GameId,
            mGameState = GameState.INITIALIZING,
            mGameProtocolVersion = 1,
            mHostNetworkAddress = createGameRequest.mHostNetworkAddress,
            mTopologyHostSessionId = (uint)host.UserId,
            mIgnoreEntryCriteriaWithInvite = true,
            mMeshAttribs = createGameRequest.mMeshAttribs,
            mMaxPlayerCapacity = createGameRequest.mMaxPlayerCapacity,
            mNetworkQosData = host.NetworkInfo.mQosData,
            mNetworkTopology = GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED,
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
            mUUID = "game" + GameId,
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = createGameRequest.mGameProtocolVersionString,
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            }
        };
        Manager.ZamboniGames.Add(this);
    }

    public ZamboniGame(ZamboniUser host, StartMatchmakingRequest startMatchmakingRequest, string gameMode)
    {
        GameId = Program.Database.GetNextGameId();
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
                (uint)host.UserId
            },
            mGameAttribs = mGameAttribs,
            mSlotCapacities = Capacities(gameMode),
            // mEntryCriteriaMap = startMatchmakingRequest.mEntryCriteriaMap,
            mGameId = GameId,
            mGameName = "game" + GameId,
            mGameSettings = startMatchmakingRequest.mGameSettings,
            mGameReportingId = GameId,
            mGameState = GameState.INITIALIZING,
            mGameProtocolVersion = 1,
            mHostNetworkAddress = host.NetworkInfo.mAddress,
            mTopologyHostSessionId = (uint)host.UserId,
            mIgnoreEntryCriteriaWithInvite = true,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mMaxPlayerCapacity = 2, //TODO Parse from gamemode
            mNetworkQosData = host.NetworkInfo.mQosData,
            mNetworkTopology = GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED,
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
            mUUID = "game" + GameId,
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = startMatchmakingRequest.mGameProtocolVersionString,
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            }
        };
        Manager.ZamboniGames.Add(this);
    }

    public uint GameId { get; }

    public List<ZamboniUser> ZamboniUsers { get; } = new();

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

    public void AddGameParticipant(ZamboniUser user, uint matchmakingSessionId = 0)
    {
        //TODO Lobby capacities?
        ZamboniUsers.Add(user);
        var replicatedGamePlayer = user.ToReplicatedGamePlayer((byte)(ZamboniUsers.Count - 1), GameId);
        ReplicatedGamePlayers.Add(replicatedGamePlayer);

        GameManagerBase.Server.NotifyJoinGameAsync(user.BlazeServerConnection, new NotifyJoinGame
        {
            mJoinErr = 0,
            mGameData = ReplicatedGameData,
            mMatchmakingSessionId = matchmakingSessionId,
            mGameRoster = ReplicatedGamePlayers
        });
        NotifyParticipants(new NotifyPlayerJoining
        {
            mGameId = GameId,
            mJoiningPlayer = replicatedGamePlayer
        });
    }

    public void RemoveGameParticipant(ZamboniUser user)
    {
        ZamboniUsers.Remove(user);
        ReplicatedGamePlayers.Remove(ReplicatedGamePlayers.Find(player => player.mPlayerId.Equals((uint)user.UserId)));
        NotifyParticipants(new NotifyPlayerRemoved
        {
            mPlayerRemovedTitleContext = 0, //??
            mGameId = GameId,
            mPlayerId = (uint)user.UserId,
            mPlayerRemovedReason = PlayerRemovedReason.PLAYER_LEFT
        });
        if (ZamboniUsers.Count == 1)
            NotifyParticipants(new NotifyPlayerRemoved
            {
                mPlayerRemovedTitleContext = 0, //??
                mGameId = GameId,
                mPlayerId = (uint)ZamboniUsers[0].UserId,
                mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED
            });

        Manager.ZamboniGames.Remove(this);
    }

    public void NotifyParticipants(NotifyGamePlayerStateChange playerStateChange)
    {
        foreach (var zamboniUser in ZamboniUsers) GameManagerBase.Server.NotifyGamePlayerStateChangeAsync(zamboniUser.BlazeServerConnection, playerStateChange);
    }

    public void NotifyParticipants(NotifyPlayerJoinCompleted playerJoinCompleted)
    {
        foreach (var zamboniUser in ZamboniUsers) GameManagerBase.Server.NotifyPlayerJoinCompletedAsync(zamboniUser.BlazeServerConnection, playerJoinCompleted);
    }

    public void NotifyParticipants(NotifyPlayerRemoved playerRemoved)
    {
        foreach (var zamboniUser in ZamboniUsers) GameManagerBase.Server.NotifyPlayerRemovedAsync(zamboniUser.BlazeServerConnection, playerRemoved);
    }

    private void NotifyParticipants(NotifyPlayerJoining playerJoining)
    {
        foreach (var zamboniUser in ZamboniUsers) GameManagerBase.Server.NotifyPlayerJoiningAsync(zamboniUser.BlazeServerConnection, playerJoining);
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
        SortedDictionary<string, string> vsGameAttribs = new();
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
               string.Join(", ", ZamboniUsers.Select(zamboniUser => zamboniUser.Username)) +
               " gameId:" + GameId +
               " state: " + ReplicatedGameData.mGameState +
               " type (1 vs game 2 shootout, 3 otp): " + ReplicatedGameData.mGameAttribs["OSDK_gameMode"];
    }
}
using System.Collections.Generic;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;

namespace Zamboni;

public class ZamboniGame
{
    public ZamboniGame(ZamboniUser host, ZamboniUser notHost)
    {
        GameId = Program.Database.GetNextGameId();
        ZamboniUsers.Add(host);
        ZamboniUsers.Add(notHost);
        ReplicatedGamePlayers.Add(host.ToReplicatedGamePlayer(0, GameId));
        ReplicatedGamePlayers.Add(notHost.ToReplicatedGamePlayer(1, GameId));
        ReplicatedGameData = CreateZamboniRankedGameData(host, notHost);
        Manager.ZamboniGames.Add(this);
    }

    public ZamboniGame(ZamboniUser host, CreateGameRequest createGameRequest)
    {
        GameId = Program.Database.GetNextGameId();
        ZamboniUsers.Add(host);
        ReplicatedGamePlayers.Add(host.ToReplicatedGamePlayer(0, GameId));
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
            mGameProtocolVersion = createGameRequest.mGameProtocolVersion,
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

    public uint GameId { get; }

    public List<ZamboniUser> ZamboniUsers { get; } = new();

    public ReplicatedGameData ReplicatedGameData { get; set; }
    public List<ReplicatedGamePlayer> ReplicatedGamePlayers { get; set; } = new();

    public void AddGameParticipant(ZamboniUser user)
    {
        //TODO Lobby capacities?
        ZamboniUsers.Add(user);
        var replicatedGamePlayer = user.ToReplicatedGamePlayer((byte)(ZamboniUsers.Count - 1), GameId);
        ReplicatedGamePlayers.Add(replicatedGamePlayer);

        GameManagerBase.Server.NotifyJoinGameAsync(user.BlazeServerConnection, new NotifyJoinGame
        {
            mJoinErr = 0,
            mGameData = ReplicatedGameData,
            mMatchmakingSessionId = 0,
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
        if (ZamboniUsers.Count != 1) return;
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

    private ReplicatedGameData CreateZamboniRankedGameData(ZamboniUser host)
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
            mGameId = GameId,
            mGameName = "game" + GameId,
            mGameSettings = GameSettings.OpenToJoinByPlayer,
            mGameReportingId = GameId,
            mGameState = GameState.INITIALIZING,
            mGameProtocolVersion = 1,
            mHostNetworkAddress = host.NetworkInfo.mAddress,
            mTopologyHostSessionId = (uint)host.UserId,
            mIgnoreEntryCriteriaWithInvite = true,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mMaxPlayerCapacity = 2,
            mNetworkQosData = host.NetworkInfo.mQosData,
            mNetworkTopology = GameNetworkTopology.CLIENT_SERVER_PEER_HOSTED,
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = (uint)host.UserId,
                mSlotId = 0
            },
            mPingSiteAlias = "qos",
            mQueueCapacity = 4,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = (uint)host.UserId,
                mSlotId = 0
            },

            mUUID = "game" + GameId,
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

    private ReplicatedGameData CreateZamboniRankedShootoutGameData(ZamboniUser host)
    {
        var replicatedGameData = CreateZamboniRankedGameData(host);
        replicatedGameData.mGameAttribs = new SortedDictionary<string, string>
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
        return replicatedGameData;
    }

    public override string ToString()
    {
        return "Players: " + 
               string.Join(", ", ZamboniUsers.Select(zamboniUser => zamboniUser.Username)) + 
               " gameId:" + GameId + 
               " state: " + ReplicatedGameData.mGameState + 
               " type (1 ranked game 2 shootout): " + ReplicatedGameData.mGameAttribs["OSDK_gameMode"];    }
}
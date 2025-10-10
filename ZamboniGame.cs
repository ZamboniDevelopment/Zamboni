using System.Collections.Generic;
using System.Text;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.GameManager;

namespace Zamboni;

public class ZamboniGame
{
    private static uint _gameIdCounter;

    public ZamboniGame(ZamboniUser host, ZamboniUser notHost)
    {
        GameId = _gameIdCounter++;
        ZamboniUsers.Add(host);
        ZamboniUsers.Add(notHost);
        ReplicatedGamePlayers.Add(host.ToReplicatedGamePlayer(0, GameId));
        ReplicatedGamePlayers.Add(notHost.ToReplicatedGamePlayer(1, GameId));
        ReplicatedGameData = CreateZamboniRankedGameData(host);
    }

    public ZamboniGame(ZamboniUser host)
    {
        GameId = _gameIdCounter++;
        ZamboniUsers.Add(host);
        ReplicatedGamePlayers.Add(host.ToReplicatedGamePlayer(0, GameId));
        ReplicatedGameData = CreateZamboniRankedGameData(host);
    }

    public uint GameId { get; }

    public List<ZamboniUser> ZamboniUsers { get; } = new();

    public ReplicatedGameData ReplicatedGameData { get; set; }
    public List<ReplicatedGamePlayer> ReplicatedGamePlayers { get; set; } = new();

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
                2, 2, 2, 2
            }, //TODO
            mEntryCriteriaMap = new SortedDictionary<string, string>(),
            mGameId = GameId,
            mGameName = "game" + GameId,
            mGameSettings = GameSettings.OpenToJoinByPlayer,
            mGameReportingId = 0,
            mGameState = GameState.INITIALIZING,
            mGameProtocolVersionHash = GetGameProtocolVersionHash(), //Client doesnt seem to read this
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

    public override string ToString()
    {
        return base.ToString();
    }
}
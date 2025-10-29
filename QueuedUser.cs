using Blaze2SDK.Blaze.GameManager;

namespace Zamboni;

public class QueuedUser
{
    private static uint _nextId = 1;

    public QueuedUser(ZamboniUser zamboniUser, StartMatchmakingRequest startMatchmakingRequest)
    {
        ZamboniUser = zamboniUser;
        MatchmakingSessionId = _nextId++;
        StartMatchmakingRequest = startMatchmakingRequest;
    }

    public ZamboniUser ZamboniUser { get; }
    public uint MatchmakingSessionId { get; }
    public StartMatchmakingRequest StartMatchmakingRequest { get; }
}
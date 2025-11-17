using System.Collections.Generic;
using System.Linq;
using BlazeCommon;

namespace Zamboni;

public static class ServerManager
{
    private static readonly List<ServerPlayer> ServerPlayers = new();
    private static readonly List<QueuedPlayer> QueuedPlayers = new();
    private static readonly List<ServerGame> ServerGames = new();

    public static void AddServerPlayer(ServerPlayer serverPlayer)
    {
        var existing = GetServerPlayer(serverPlayer.UserIdentification.mName);
        if (existing != null)
        {
            RemoveServerPlayer(existing);
        }
        ServerPlayers.Add(serverPlayer);
    }

    public static void AddQueuedPlayer(QueuedPlayer queuedPlayer)
    {
        QueuedPlayers.Add(queuedPlayer);
    }

    public static void AddServerGame(ServerGame serverGame)
    {
        ServerGames.Add(serverGame);
    }

    public static bool RemoveServerPlayer(ServerPlayer serverPlayer)
    {
        return ServerPlayers.Remove(serverPlayer);
    }

    public static bool RemoveQueuedPlayer(QueuedPlayer queuedPlayer)
    {
        return QueuedPlayers.Remove(queuedPlayer);
    }

    public static bool RemoveServerGame(ServerGame serverGame)
    {
        return ServerGames.Remove(serverGame);
    }

    public static List<ServerPlayer> GetServerPlayers()
    {
        return ServerPlayers;
    }

    public static List<QueuedPlayer> GetQueuedPlayers()
    {
        return QueuedPlayers;
    }

    public static List<ServerGame> GetServerGames()
    {
        return ServerGames;
    }

    public static ServerPlayer GetServerPlayer(BlazeServerConnection blazeServerConnection)
    {
        return ServerPlayers.FirstOrDefault(serverPlayer => serverPlayer.BlazeServerConnection.Equals(blazeServerConnection));
    }

    public static ServerPlayer GetServerPlayer(ProtoFireConnection protoFireConnection)
    {
        return ServerPlayers.FirstOrDefault(serverPlayer => serverPlayer.BlazeServerConnection.ProtoFireConnection.Equals(protoFireConnection));
    }

    public static ServerPlayer GetServerPlayer(uint userId)
    {
        return ServerPlayers.FirstOrDefault(serverPlayer => serverPlayer.UserIdentification.mBlazeId.Equals(userId));
    }

    public static ServerPlayer GetServerPlayer(ulong messengerId)
    {
        return ServerPlayers.FirstOrDefault(serverPlayer => serverPlayer.MessengerId.Equals(messengerId));
    }

    public static ServerPlayer GetServerPlayer(string name)
    {
        return ServerPlayers.FirstOrDefault(serverPlayer => serverPlayer.UserIdentification.mName.Equals(name));
    }

    public static ServerGame GetServerGame(uint id)
    {
        return ServerGames.FirstOrDefault(serverGame => serverGame.ReplicatedGameData.mGameId.Equals(id));
    }

    public static ServerGame GetServerGame(ServerPlayer serverPlayer)
    {
        return ServerGames.FirstOrDefault(serverGame => serverGame.ServerPlayers.Contains(serverPlayer));
    }

    public static QueuedPlayer GetQueuedPlayer(ServerPlayer serverPlayer)
    {
        return QueuedPlayers.FirstOrDefault(queuedPlayer => queuedPlayer.ServerPlayer.Equals(serverPlayer));
    }
}
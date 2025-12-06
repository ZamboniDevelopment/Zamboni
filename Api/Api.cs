using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using Zamboni.Models;

namespace Zamboni;

public class Api
{
    private readonly string address;

    public Api(string address = "http://0.0.0.0:8080")
    {
        this.address = address;
    }

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(address);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<IConnectionMultiplexer?>(_ =>
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[Redis] Connecting...");
                Console.ResetColor();

                var mux = ConnectionMultiplexer.Connect(Program.ZamboniConfig.RedisConnectionString);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Redis] Connected successfully.");
                Console.ResetColor();

                return mux;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Redis WARNING] Failed to connect to Redis! Caching disabled.");
                Console.WriteLine($"Reason: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        });

        builder.Services.AddSingleton(_ =>
            new FixedWindowRateLimiter(
                permitLimit: 120,
                window: TimeSpan.FromMinutes(1),
                queueLimit: 10
            ));

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            var limiter = context.RequestServices.GetRequiredService<FixedWindowRateLimiter>();

            if (!await limiter.AllowRequestAsync())
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new { message = "Too Many Requests" });
                return;
            }

            await next();
        });

        app.MapGet("/nhl10/status", () => Results.Json(new
        {
            serverVersion = Program.Name,
            onlineUsersCount = ServerManager.GetServerPlayers().Count,
            onlineUsers = string.Join(", ",
                ServerManager.GetServerPlayers().Select(p => p.UserIdentification.mName)),
            queuedUsers = ServerManager.GetQueuedPlayers().Count,
            activeGames = ServerManager.GetServerGames().Count
        }));

        app.MapGet("/nhl10/api/players", async () =>
        {
            var list = new List<string>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT DISTINCT gamertag FROM reports", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));

            return Results.Json(list);
        });

        app.MapGet("/nhl10/api/player/{gamertag}", async (string gamertag) =>
        {
            var reports = new List<(int UserId, int Score)>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "SELECT user_id, score FROM reports WHERE gamertag = @gamertag",
                conn);

            cmd.Parameters.AddWithValue("@gamertag", gamertag);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                reports.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }

            if (reports.Count == 0)
                return Results.NotFound(new { message = $"No reports found for gamertag {gamertag}" });

            var user_id = reports[0].UserId;
            var profile = new PlayerProfile(
                user_id,
                gamertag,
                totalGames: reports.Count,
                totalGoals: reports.Sum(r => r.Score)
            );

            return Results.Json(profile);
        });

        app.MapGet("/nhl10/api/raw/games", async () =>
        {
            var list = new List<Dictionary<string, object?>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT * FROM games", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                list.Add(row);
            }

            return Results.Json(list);
        });

        app.MapGet("/nhl10/api/raw/reports", async () =>
        {
            var list = new List<Dictionary<string, object?>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT * FROM reports", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                list.Add(row);
            }

            return Results.Json(list);
        });

        app.MapGet("/nhl10/api/games", async () =>
        {
            var games = new List<Game>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT game_id, played_at, home_team, away_team, home_score, away_score
                FROM games
                ORDER BY played_at DESC
            ", conn);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                games.Add(new Game
                {
                    GameId = reader.GetInt32(0),
                    PlayedAt = reader.GetDateTime(1),
                    HomeTeam = reader.GetString(2),
                    AwayTeam = reader.GetString(3),
                    HomeScore = reader.GetInt32(4),
                    AwayScore = reader.GetInt32(5)
                });
            }

            return Results.Json(games);
        });

        app.MapGet("/nhl10/api/game/{id:int}/reports", async (int id) =>
        {
            var reports = new List<ReportEntry>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT user_id, gamertag, score
                FROM reports
                WHERE game_id = @id
            ", conn);

            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reports.Add(new ReportEntry
                {
                    UserId = reader.GetInt32(0),
                    Gamertag = reader.GetString(1),
                    Score = reader.GetInt32(2)
                });
            }

            return Results.Json(new CombinedGameReport
            {
                GameId = id,
                Reports = reports
            });
        });

        app.MapGet("/nhl10/api/leaderboard/{range}", async (string range, IConnectionMultiplexer? redis) =>
        {
            var db = redis.GetSafeDb();
            var key = $"nhl10:leaderboard:{range}";

            if (db != null)
            {
                var cached = await db.StringGetAsync(key);
                if (cached.HasValue)
                    return Results.Content(cached!, "application/json");
            }
            else
            {
                Console.WriteLine("[Redis] Skipped leaderboard cache (unavailable)");
            }

            DateTime from = range switch
            {
                "day" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                "all" => DateTime.MinValue,
                _ => DateTime.MinValue
            };

            if (from == DateTime.MinValue && range != "all")
                return Results.BadRequest(new { message = "Invalid range. Use day, week, month, or all." });

            var entries = new List<LeaderboardEntry>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT r.gamertag,
                       SUM(r.score) AS total_goals,
                       COUNT(*)     AS games_played
                FROM reports r
                JOIN games g ON g.game_id = r.game_id
                WHERE (@from = '0001-01-01'::timestamp OR g.played_at >= @from)
                GROUP BY r.gamertag
                ORDER BY total_goals DESC
            ", conn);

            cmd.Parameters.AddWithValue("@from", from);

            await using var reader = await cmd.ExecuteReaderAsync();
            int rank = 1;
            while (await reader.ReadAsync())
            {
                entries.Add(new LeaderboardEntry
                {
                    Gamertag = reader.GetString(0),
                    TotalGoals = reader.GetInt32(1),
                    GamesPlayed = reader.GetInt32(2),
                    Rank = rank++
                });
            }

            var json = JsonSerializer.Serialize(entries);

            if (db != null)
                await db.StringSetAsync(key, json, TimeSpan.FromSeconds(60));

            return Results.Json(entries);
        });

        app.MapGet("/nhl10/api/stats/global", async (IConnectionMultiplexer? redis) =>
        {
            var db = redis.GetSafeDb();

            const string key = "nhl10:stats:global";

            if (db != null)
            {
                var cached = await db.StringGetAsync(key);
                if (cached.HasValue)
                    return Results.Content(cached!, "application/json");
            }
            else
            {
                Console.WriteLine("[Redis] Skipped global stats cache");
            }

            int totalGames;
            int totalReports;
            int totalPlayers;

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM games", conn))
                totalGames = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM reports", conn))
                totalReports = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            await using (var cmd = new NpgsqlCommand("SELECT COUNT(DISTINCT gamertag) FROM reports", conn))
                totalPlayers = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            var payload = new
            {
                totalGames,
                totalReports,
                totalPlayers
            };

            var json = JsonSerializer.Serialize(payload);

            if (db != null)
                await db.StringSetAsync(key, json, TimeSpan.FromSeconds(60));

            return Results.Json(payload);
        });

        app.MapGet("/nhl10/api/stats/active", async (IConnectionMultiplexer? redis) =>
        {
            var db = redis.GetSafeDb();

            const string key = "nhl10:stats:active";

            if (db != null)
            {
                var cached = await db.StringGetAsync(key);
                if (cached.HasValue)
                    return Results.Content(cached!, "application/json");
            }
            else
            {
                Console.WriteLine("[Redis] Skipped active stats cache");
            }

            var payload = new
            {
                serverVersion = Program.Name,
                onlineUsersCount = ServerManager.GetServerPlayers().Count,
                queuedUsers = ServerManager.GetQueuedPlayers().Count,
                activeGames = ServerManager.GetServerGames().Count
            };

            var json = JsonSerializer.Serialize(payload);

            if (db != null)
                await db.StringSetAsync(key, json, TimeSpan.FromSeconds(10));

            return Results.Json(payload);
        });
        
        app.MapGet("/nhl10/api/reports/latest", async (int? limit) =>
        {
            var max = Math.Clamp(limit ?? 50, 1, 500);
            var list = new List<Dictionary<string, object?>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT *
                FROM reports
                ORDER BY id DESC
                LIMIT @limit
            ", conn);

            cmd.Parameters.AddWithValue("@limit", max);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                list.Add(row);
            }

            return Results.Json(list);
        });
        
        app.MapGet("/nhl10/api/user/{id:int}/history", async (int id) =>
        {
            var list = new List<Dictionary<string, object?>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT r.*, g.played_at, g.home_team, g.away_team, g.home_score, g.away_score
                FROM reports r
                JOIN games g ON g.game_id = r.game_id
                WHERE r.user_id = @id
                ORDER BY g.played_at DESC
            ", conn);

            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                list.Add(row);
            }

            return Results.Json(list);
        });

        app.MapGet("/nhl10/api/games/{id:int}/summary", async (int id) =>
        {
            Game? game = null;
            var reports = new List<ReportEntry>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using (var gameCmd = new NpgsqlCommand(@"
                SELECT game_id, played_at, home_team, away_team, home_score, away_score
                FROM games
                WHERE game_id = @id
            ", conn))
            {
                gameCmd.Parameters.AddWithValue("@id", id);

                await using var reader = await gameCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    game = new Game
                    {
                        GameId = reader.GetInt32(0),
                        PlayedAt = reader.GetDateTime(1),
                        HomeTeam = reader.GetString(2),
                        AwayTeam = reader.GetString(3),
                        HomeScore = reader.GetInt32(4),
                        AwayScore = reader.GetInt32(5)
                    };
                }
            }

            if (game is null)
                return Results.NotFound(new { message = $"Game {id} not found" });

            await using (var reportCmd = new NpgsqlCommand(@"
                SELECT user_id, gamertag, score
                FROM reports
                WHERE game_id = @id
            ", conn))
            {
                reportCmd.Parameters.AddWithValue("@id", id);

                await using var reader = await reportCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reports.Add(new ReportEntry
                    {
                        UserId = reader.GetInt32(0),
                        Gamertag = reader.GetString(1),
                        Score = reader.GetInt32(2)
                    });
                }
            }

            string? winnerTeam = null;
            if (game.HomeScore > game.AwayScore)
                winnerTeam = game.HomeTeam;
            else if (game.AwayScore > game.HomeScore)
                winnerTeam = game.AwayTeam;

            return Results.Json(new
            {
                game,
                reports,
                winnerTeam
            });
        });
        
        await app.RunAsync();
    }
}

public class FixedWindowRateLimiter
{
    private readonly int _permitLimit;
    private readonly TimeSpan _window;
    private readonly int _queueLimit;

    private readonly ConcurrentQueue<DateTime> _requests = new();

    public FixedWindowRateLimiter(int permitLimit, TimeSpan window, int queueLimit)
    {
        _permitLimit = permitLimit;
        _window = window;
        _queueLimit = queueLimit;
    }

    public Task<bool> AllowRequestAsync()
    {
        var now = DateTime.UtcNow;

        while (_requests.TryPeek(out var ts) && now - ts > _window)
            _requests.TryDequeue(out _);

        if (_requests.Count >= _permitLimit + _queueLimit)
            return Task.FromResult(false);

        _requests.Enqueue(now);
        return Task.FromResult(true);
    }
}

public static class RedisExtensions
{
    public static IDatabase? GetSafeDb(this IConnectionMultiplexer? multiplexer)
    {
        if (multiplexer == null)
            return null;

        if (!multiplexer.IsConnected)
            return null;

        return multiplexer.GetDatabase();
    }
}

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
using Zamboni.Api.Models;

namespace Zamboni.Api;

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

            await using var cmd = new NpgsqlCommand("SELECT DISTINCT gamertag FROM reports WHERE gamertag IS NOT NULL", conn);
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
                var userId = reader.GetInt32(0);
                var score = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                reports.Add((userId, score));
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
            var list = new List<object>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var gamesCmd = new NpgsqlCommand("SELECT * FROM games ORDER BY created_at DESC", conn);
            var reportsCmd = new NpgsqlCommand("SELECT * FROM reports", conn);

            var rawGames = new List<Dictionary<string, object?>>();
            var rawReports = new List<Dictionary<string, object?>>();

            await using (var r = await gamesCmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < r.FieldCount; i++)
                        row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                    rawGames.Add(row);
                }
            }

            await using (var r = await reportsCmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < r.FieldCount; i++)
                        row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                    rawReports.Add(row);
                }
            }
            
            var grouped = rawReports
                .Where(r => r.ContainsKey("game_id"))
                .GroupBy(r => Convert.ToInt32(r["game_id"]))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var game in rawGames)
            {
                int gameId = Convert.ToInt32(game["game_id"]);
                grouped.TryGetValue(gameId, out var reps);

                reps ??= new();

                var obj = new
                {
                    game_id = gameId,
                    fnsh = game.ContainsKey("fnsh") ? game["fnsh"] : null,
                    gtyp = game.ContainsKey("gtyp") ? game["gtyp"] : null,
                    venue = game.ContainsKey("venue") ? game["venue"] : null,
                    created_at = game["created_at"],
                    players = reps.Count,
                    totalGoals = reps.Sum(r => Convert.ToInt32(r["score"] ?? 0)),
                    avgFps = reps.Any() ? reps.Average(r => Convert.ToInt32(r["fpsavg"] ?? 0)) : 0,
                    avgLatency = reps.Any() ? reps.Average(r => Convert.ToInt32(r["lateavgnet"] ?? 0)) : 0,
                    teams = reps.Select(r => new
                    {
                        team_name = r["team_name"],
                        score = r["score"],
                        shots = r["shots"],
                        hits = r["hits"],
                        gamertag = r["gamertag"]
                    }).ToList(),
                    status = Convert.ToBoolean(game["fnsh"] ?? false) ? "Finished" : "In Progress"
                };

                list.Add(obj);
            }

            return Results.Json(list);
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
                    Gamertag = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Score = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
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
                WHERE (@from = '0001-01-01'::timestamp OR g.created_at >= @from)
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
                    Gamertag = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    TotalGoals = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    GamesPlayed = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
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
                ORDER BY created_at DESC
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
            var history = new List<Dictionary<string, object?>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();
            
            var userCmd = new NpgsqlCommand("SELECT * FROM reports WHERE user_id = @id ORDER BY created_at DESC", conn);
            userCmd.Parameters.AddWithValue("@id", id);

            var userReports = new List<Dictionary<string, object?>>();
            await using (var reader = await userCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    userReports.Add(row);
                }
            }

            if (!userReports.Any())
                return Results.Json(history);

            var gameIds = userReports.Select(r => Convert.ToInt32(r["game_id"])).ToArray();
            var oppCmd = new NpgsqlCommand($@"
                SELECT * FROM reports 
                WHERE game_id = ANY(@gids) AND user_id != @id", conn);

            oppCmd.Parameters.AddWithValue("@gids", gameIds);
            oppCmd.Parameters.AddWithValue("@id", id);

            var opponents = new List<Dictionary<string, object?>>();
            await using (var reader2 = await oppCmd.ExecuteReaderAsync())
            {
                while (await reader2.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader2.FieldCount; i++)
                        row[reader2.GetName(i)] = reader2.IsDBNull(i) ? null : reader2.GetValue(i);
                    opponents.Add(row);
                }
            }

            foreach (var r in userReports)
            {
                int gid = Convert.ToInt32(r["game_id"]);
                var opp = opponents.FirstOrDefault(x => Convert.ToInt32(x["game_id"]) == gid);

                r["opponent"] = opp?["gamertag"];
                r["opponent_team"] = opp?["team_name"];
                r["opponent_score"] = opp?["score"];
                r["opponent_hits"] = opp?["hits"];
                r["opponent_shots"] = opp?["shots"];

                history.Add(r);
            }

            return Results.Json(history);
        });

        app.MapGet("/nhl10/api/games/{id:int}/summary", async (int id) =>
        {
            Game? game = null;
            var reports = new List<ReportEntry>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using (var gameCmd = new NpgsqlCommand(@"
                SELECT 
                    g.game_id::int AS game_id,
                    g.created_at AS played_at,
                    MAX(CASE WHEN r.team = 1 THEN r.team_name END) AS home_team,
                    MAX(CASE WHEN r.team = 0 THEN r.team_name END) AS away_team,
                    COALESCE(SUM(CASE WHEN r.team = 1 THEN r.score END), 0) AS home_score,
                    COALESCE(SUM(CASE WHEN r.team = 0 THEN r.score END), 0) AS away_score
                FROM games g
                LEFT JOIN reports r ON r.game_id = g.game_id
                WHERE g.game_id = @id
                GROUP BY g.game_id, g.created_at
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
                        HomeTeam = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2),
                        AwayTeam = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
                        HomeScore = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        AwayScore = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
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
                        Gamertag = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Score = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
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

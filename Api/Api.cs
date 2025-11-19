using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Npgsql;
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
        var app = builder.Build();

        app.MapGet("/nhl10/status", () => Results.Json(new
        {
            serverVersion = Program.Name,
            onlineUsersCount = ServerManager.GetServerPlayers().Count,
            onlineUsers = string.Join(", ", ServerManager.GetServerPlayers().Select(serverPlayer => serverPlayer.UserIdentification.mName)),
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

            while (await reader.ReadAsync()) list.Add(reader.GetString(0));

            return Results.Json(list);
        });
        app.MapGet("/nhl10/api/player/{gamertag}", async (string gamertag) =>
        {
            var reports = new List<(int UserId, int Score)>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(
                "SELECT user_id, score FROM reports WHERE gamertag = @gamertag",
                conn
            );

            cmd.Parameters.AddWithValue("@gamertag", gamertag);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var userId = reader.GetInt32(reader.GetOrdinal("user_id"));
                var score = reader.GetInt32(reader.GetOrdinal("score"));

                reports.Add((userId, score));
            }

            if (reports.Count == 0)
                throw new Exception($"No reports found for gamertag {gamertag}");

            var user_id = reports[0].UserId;
            var totalGames = reports.Count;
            var totalGoals = reports.Sum(r => r.Score);

            return new PlayerProfile(user_id, gamertag, totalGames, totalGoals);
        });

        app.MapGet("/nhl10/api/raw/games", async () =>
        {
            var list = new List<Dictionary<string, object>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT * FROM games", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value;
                }

                list.Add(row);
            }

            return Results.Json(list);
        });

        app.MapGet("/nhl10/api/raw/reports", async () =>
        {
            var list = new List<Dictionary<string, object>>();

            await using var conn = new NpgsqlConnection(Program.ZamboniConfig.DatabaseConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT * FROM reports", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value;
                }

                list.Add(row);
            }

            return Results.Json(list);
        });

        await app.RunAsync(address);
    }
}
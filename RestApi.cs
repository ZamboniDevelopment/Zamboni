using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Zamboni;

public class RestApi
{
    private readonly string address;

    public RestApi(string address = "http://0.0.0.0:8080")
    {
        this.address = address;
    }

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/status", () => Results.Json(new
        {
            serverVersion = Program.Version,
            onlineUsersCount = Manager.ZamboniUsers.Count,
            onlineUsers = string.Join(", ", Manager.ZamboniUsers.Select(zamboniUser => zamboniUser.Username)),
            queuedUsers = Manager.QueuedUsers.Count,
            activeGames = Manager.ZamboniGames.Count
        }));

        await app.RunAsync(address);
    }
}
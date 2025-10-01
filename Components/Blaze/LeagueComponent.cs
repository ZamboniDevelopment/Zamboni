using System.Threading.Tasks;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class LeagueComponent : LeagueComponentBase.Server
{
    public override Task<NullStruct> GetLeaguesByUserAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
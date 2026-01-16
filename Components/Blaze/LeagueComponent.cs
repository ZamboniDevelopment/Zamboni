using System.Threading.Tasks;
using Blaze2SDK.Blaze.Example;
using Blaze2SDK.Blaze.League;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class LeagueComponent : LeagueComponentBase.Server
{
    public override Task<FindLeaguesResponse> GetLeaguesByUserAsync(GetLeaguesByUserRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new FindLeaguesResponse());
    }

    public override Task<NullStruct> GetInvitationsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
using System.Threading.Tasks;
using Blaze2SDK.Blaze.Clubs;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class ClubsComponent : ClubsComponentBase.Server
{
    public override Task<ClubsComponentSettings> GetClubsComponentSettingsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new ClubsComponentSettings());
    }

    public override Task<NullStruct> GetPetitionsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }


    public override Task<FindClubsResponse> FindClubsAsync(FindClubsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new FindClubsResponse());
    }

    public override Task<NullStruct> UpdateMemberOnlineStatusAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> GetInvitationsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
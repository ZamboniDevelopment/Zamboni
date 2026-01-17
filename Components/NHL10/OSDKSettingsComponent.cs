using System.Threading.Tasks;
using BlazeCommon;
using Zamboni.Components.NHL10.Bases;
using Zamboni.Components.NHL10.Structs;

namespace Zamboni.Components.NHL10;

internal class OSDKSettingsComponent : OSDKSettingsComponentBase.Server
{
    public override Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchSettingsResponse());
    }

    public override Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchSettingsGroupsResponse());
    }
}
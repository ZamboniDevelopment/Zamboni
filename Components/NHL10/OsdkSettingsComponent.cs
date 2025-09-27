using System.Threading.Tasks;
using BlazeCommon;
using Zamboni.Components.NHL10.Bases;

namespace Zamboni.Components.NHL10;

internal class OsdkSettingsComponent : OsdkSettingsComponentBase.Server
{
    public override Task<NullStruct> fetchSettings(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> fetchSettingsGroups(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
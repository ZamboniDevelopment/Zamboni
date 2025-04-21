using System.Threading.Tasks;
using Blaze2SDK.Blaze;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

public class UserSessionsComponent : UserSessionsBase.Server
{
    public override Task<NullStruct> UpdateNetworkInfoAsync(NetworkInfo request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
        });
    }
    public override Task<NullStruct> UpdateHardwareFlagsAsync(UpdateHardwareFlagsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
        });
    }


}
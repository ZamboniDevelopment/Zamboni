using System.Threading.Tasks;
using BlazeCommon;
using Zamboni.Components.NHL10.Bases;
using Zamboni.Components.NHL10.Structs;

namespace Zamboni.Components.NHL10;

internal class DynamicMessagingComponent : DynamicMessagingComponentBase.Server
{
    public override Task<NullStruct> GetMessagesAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<DynamicConfigResponse> GetDynamicConfigAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new DynamicConfigResponse
        {
            mDataRequestDelay = 100,
            mErrorRetryDelay = 100,
            mMessageDelayInterval = 100,
            mMaximumMessageCount = 10
        });
    }
}
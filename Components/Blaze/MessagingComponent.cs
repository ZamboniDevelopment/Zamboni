using System.Threading.Tasks;
using Blaze2SDK.Blaze.Messaging;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class MessagingComponent : MessagingComponentBase.Server
{
    public override Task<FetchMessageResponse> FetchMessagesAsync(FetchMessageRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchMessageResponse
        {
            mCount = 0
        });
    }

}
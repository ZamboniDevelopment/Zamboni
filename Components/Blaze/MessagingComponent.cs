using System.Threading.Tasks;
using Blaze2SDK.Blaze.Messaging;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class MessagingComponent : MessagingComponentBase.Server
{
    private static uint _messageIdCounter = 1;

    public override Task<FetchMessageResponse> FetchMessagesAsync(FetchMessageRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchMessageResponse
        {
            mCount = 0
        });
    }

    public override Task<PurgeMessageResponse> PurgeMessagesAsync(PurgeMessageRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new PurgeMessageResponse
        {
            mCount = 0
        });
    }

    public override Task<SendMessageResponse> SendMessageAsync(ClientMessage request, BlazeRpcContext context)
    {
        var messageId = ++_messageIdCounter;
        var target = ServerManager.GetServerPlayer(request.mTarget);

        //If target is offline, we should store the message and send it when he comes online (Not a priority)
        if (target == null)
            return Task.FromResult(new SendMessageResponse
            {
                mMessageId = messageId
            });

        NotifyMessageAsync(target.BlazeServerConnection, new ServerMessage
        {
            mFlags = 0,
            mMessageId = messageId,
            mSourceName = ServerManager.GetServerPlayer(context.BlazeConnection).UserIdentification.mName,
            mPayload = request,
            mSource = ServerManager.GetServerPlayer(context.BlazeConnection).MessengerId,
            mTimestamp = 0
        });

        return Task.FromResult(new SendMessageResponse
        {
            mMessageId = messageId
        });
    }
}
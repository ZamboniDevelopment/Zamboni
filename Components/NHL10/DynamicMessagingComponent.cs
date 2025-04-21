using System.Threading.Tasks;
using BlazeCommon;
using Zamboni.Components.NHL10.Bases;

namespace Zamboni.Components.NHL10;

internal class DynamicMessagingComponent : DynamicMessagingComponentBase.Server
{
    public override Task<NullStruct> getMessages(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
    
    public override Task<NullStruct> getConfig(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
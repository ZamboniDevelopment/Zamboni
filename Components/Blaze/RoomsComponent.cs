using System.Threading.Tasks;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class RoomsComponent : RoomsComponentBase.Server
{
    public override Task<NullStruct> SelectViewUpdatesAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
            
        });
    }
    
    public override Task<NullStruct> SelectCategoryUpdatesAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
            
        });
    }
    
    public override Task<NullStruct> ToggleJoinedRoomNotificationsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
            
        });
    }
}
using System.Threading.Tasks;
using Blaze2SDK.Blaze.GameReporting;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class GameReportingComponent : GameReportingComponentBase.Server
{
    public override Task<NullStruct> SubmitGameReportAsync(GameReport request, BlazeRpcContext context)
    {
        if (Program.Database.isEnabled) Program.Database.InsertReport(request);
        return Task.FromResult(new NullStruct());
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Blaze2SDK.Blaze.Stats;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class StatsComponent : StatsComponentBase.Server
{
    public override Task<KeyScopes> GetKeyScopesMapAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new KeyScopes
        {
            mKeyScopesMap = new SortedDictionary<string, KeyScopeItem>()
        });
    }
}
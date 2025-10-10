using System.Threading.Tasks;
using Blaze2SDK.Blaze.Redirector;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

internal class RedirectorComponent : RedirectorComponentBase.Server
{
    public override Task<ServerInstanceInfo> GetServerInstanceAsync(ServerInstanceRequest request, BlazeRpcContext context)
    {
        var responseData = new ServerInstanceInfo
        {
            mAddress = new ServerAddress
            {
                IpAddress = new IpAddress
                {
                    mHostname = Program.PublicIp,
                    mIp = Util.GetIPAddressAsUInt(Program.PublicIp),
                    mPort = Program.ZamboniConfig.GameServerPort
                }
            },
            mSecure = false,
            mDefaultDnsAddress = 0
        };

        return Task.FromResult(responseData);
    }
}
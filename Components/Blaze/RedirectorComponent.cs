using System;
using System.Net;
using System.Threading.Tasks;
using Blaze2SDK.Blaze.Redirector;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze
{
    internal class RedirectorComponent : RedirectorComponentBase.Server
    {
        public override Task<ServerInstanceInfo> GetServerInstanceAsync(ServerInstanceRequest request,
            BlazeRpcContext context)
        {
            string stringIp = Program.ZamboniConfig.GameServerIp;
            if (stringIp.Equals("auto")) stringIp = Program.MachineIP;

            ServerInstanceInfo responseData = new ServerInstanceInfo()
            {
                mAddress = new ServerAddress()
                {
                    IpAddress = new IpAddress()
                    {
                        mHostname = stringIp,
                        mIp = GetIPAddressAsUInt(stringIp),
                        mPort = Program.ZamboniConfig.GameServerPort
                    },
                },
                mSecure = false,
                mDefaultDnsAddress = 0
            };

            return Task.FromResult(responseData);
        }

        private static uint GetIPAddressAsUInt(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                throw new ArgumentException(nameof(ipAddress));
            IPAddress address = IPAddress.Parse(ipAddress);
            byte[] bytes = address.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
    }
}
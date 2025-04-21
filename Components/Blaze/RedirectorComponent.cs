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
        public override Task<ServerInstanceInfo> GetServerInstanceAsync(ServerInstanceRequest request, BlazeRpcContext context)
        {
            ServerInstanceInfo responseData = new ServerInstanceInfo()
            {
                mAddress = new ServerAddress()
                {
                    IpAddress = new IpAddress()
                    {
                        mHostname = "127.0.0.1",
                        mIp = GetIPAddressAsUInt("127.0.0.1"),
                        mPort = 13337
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
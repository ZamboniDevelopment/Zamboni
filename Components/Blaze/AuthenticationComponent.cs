using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.Authentication;
using Blaze2SDK.Blaze.Util;
using Blaze2SDK.Components;
using BlazeCommon;
using NLog;
using XI5;

namespace Zamboni.Components.Blaze;

public class AuthenticationComponent : AuthenticationComponentBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override Task<ConsoleLoginResponse> Ps3LoginAsync(PS3LoginRequest request, BlazeRpcContext context)
    {
        XI5Ticket ticket = new XI5Ticket(request.mPS3Ticket);
        var currentTimeStamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        Logger.Warn(ticket.OnlineId + " connected");
        Program.HockeyUsers.Add(new HockeyUser(context.BlazeConnection, ticket.UserId, ticket.OnlineId));

        //NECESSARY?
        Task.Run(async () =>
        {
            var externalBlob = new List<byte>();
            externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.OnlineId.PadRight(20, '\0')));
            externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.Domain));
            externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.Region));
            externalBlob.AddRange(Encoding.ASCII.GetBytes("ps3"));
            externalBlob.Add(0x0);
            externalBlob.Add(0x1);
            externalBlob.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            await Task.Delay(500);
            UserSessionsBase.Server.NotifyUserAddedAsync(context.BlazeConnection, new UserIdentification
            {
                mAccountLocale = 1701729619,
                mExternalBlob = externalBlob.ToArray(),
                mExternalId = ticket.UserId,
                mBlazeId = (uint)ticket.UserId,
                mName = ticket.OnlineId,
                mPersonaId = ticket.OnlineId
            });
        });

        Task.Run(async () =>
        {
            await Task.Delay(1000);
            UserSessionsBase.Server.NotifyUserSessionExtendedDataUpdateAsync(context.BlazeConnection,
                new UserSessionExtendedDataUpdate
                {
                    mExtendedData = new UserSessionExtendedData
                    {
                        mAddress = new NetworkAddress
                        {
                            XboxClientAddress = null,
                            XboxServerAddress = null,
                            IpPairAddress = null,
                            IpAddress = null,
                            HostNameAddress = null
                        },
                        mBestPingSiteAlias = "qos",
                        mClientAttributes = new SortedDictionary<uint, int>(),
                        mCountry = "",
                        mDataMap = new SortedDictionary<uint, int>()
                        {
                            { 0x00070047, 0 } //???
                        },
                        mHardwareFlags = HardwareFlags.None,
                        mLatencyList = new List<int>()
                        {
                        },
                        mQosData = new NetworkQosData
                        {
                            mDownstreamBitsPerSecond = 0,
                            mNatType = NatType.NAT_TYPE_OPEN,
                            mUpstreamBitsPerSecond = 0
                        },
                        mUserInfoAttribute = 0,
                        mBlazeObjectIdList = new List<ulong>()
                        {
                        }
                    },
                    mUserId = (uint)ticket.UserId
                });
        });

        return Task.FromResult(new ConsoleLoginResponse()
        {
            mSessionInfo = new SessionInfo()
            {
                mBlazeUserId = (uint)ticket.UserId,
                mSessionKey = "session-key",
                mEmail = "",
                mPersonaDetails = new PersonaDetails()
                {
                    mDisplayName = ticket.OnlineId,
                    mLastAuthenticated = (uint)currentTimeStamp,
                    mPersonaId = (long)ticket.UserId,
                    mExtId = ticket.UserId,
                    mExtType = ExternalRefType.PS3
                },
                mUserId = (long)ticket.UserId
            },
            mTosHost = "",
            mTosUri = "",
        });
    }


    public override Task<NullStruct> LogoutAsync(NullStruct request, BlazeRpcContext context)
    {
        HockeyUser leaver = null;
        foreach (HockeyUser hockeyUser in Program.HockeyUsers)
        {
            if (hockeyUser.BlazeServerConnection.Equals(context.BlazeConnection))
            {
                leaver = hockeyUser;
                Logger.Warn(leaver.username + " disconnected");

                break;
            }
        }

        if (leaver != null) Program.HockeyUsers.Remove(leaver);
        return Task.FromResult(new NullStruct()
        {
        });
    }

    public override Task<NullStruct> CreateWalUserSessionAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct()
        {
        });
    }
}
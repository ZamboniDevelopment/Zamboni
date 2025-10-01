using System.Collections.Generic;
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
        var ticket = new XI5Ticket(request.mPS3Ticket);

        Logger.Warn(ticket.OnlineId + " connected");
        Manager.HockeyUsers.Add(new HockeyUser(context.BlazeConnection, ticket.UserId, ticket.OnlineId));

        Task.Run(async () =>
        {
            await Task.Delay(100);
            UserSessionsBase.Server.NotifyUserAddedAsync(context.BlazeConnection, new UserIdentification
            {
                mAccountLocale = 1701729619,
                mExternalId = ticket.UserId,
                mBlazeId = (uint)ticket.UserId,
                mName = ticket.OnlineId,
                mPersonaId = ticket.OnlineId
            });
        });

        Task.Run(async () =>
        {
            await Task.Delay(200);
            UserSessionsBase.Server.NotifyUserSessionExtendedDataUpdateAsync(context.BlazeConnection,
                new UserSessionExtendedDataUpdate
                {
                    mExtendedData = new UserSessionExtendedData
                    {
                        mAddress = new NetworkAddress
                        {
                            XboxClientAddress = null,
                            XboxServerAddress = null,
                            //TODO KEY POINT !!!
                            IpPairAddress = new IpPairAddress
                            {
                                ExternalAddress = new IpAddress
                                {
                                    Ip = 0,
                                    Port = 3659
                                },
                                InternalAddress = new IpAddress
                                {
                                    Ip = 0,
                                    Port = 3659
                                }
                            },
                            IpAddress = null,
                            HostNameAddress = null
                        },
                        mBestPingSiteAlias = "qos",
                        mClientAttributes = new SortedDictionary<uint, int>(),
                        mCountry = "",
                        mDataMap = new SortedDictionary<uint, int>
                        {
                            { 0x00070047, 0 } //???
                        },
                        mHardwareFlags = HardwareFlags.None,
                        mLatencyList = new List<int>(),
                        mQosData = new NetworkQosData
                        {
                            mDownstreamBitsPerSecond = 10,
                            mNatType = NatType.NAT_TYPE_OPEN,
                            mUpstreamBitsPerSecond = 10
                        },
                        mUserInfoAttribute = 0,
                        mBlazeObjectIdList = new List<ulong>()
                    },
                    mUserId = (uint)ticket.UserId
                });
        });

        return Task.FromResult(new ConsoleLoginResponse
        {
            mSessionInfo = new SessionInfo
            {
                mBlazeUserId = (uint)ticket.UserId,
                mSessionKey = ticket.UserId.ToString(),
                mEmail = "",
                mPersonaDetails = new PersonaDetails
                {
                    mDisplayName = ticket.OnlineId,
                    mLastAuthenticated = 0,
                    mPersonaId = (long)ticket.UserId,
                    mExtId = ticket.UserId,
                    mExtType = ExternalRefType.PS3
                },
                mUserId = (long)ticket.UserId
            },
            mTosHost = "",
            mTosUri = ""
        });
    }


    public override Task<NullStruct> LogoutAsync(NullStruct request, BlazeRpcContext context)
    {
        var leaver = Manager.GetHockeyUser(context.BlazeConnection);
        if (leaver != null) Manager.HockeyUsers.Remove(leaver);
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> CreateWalUserSessionAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
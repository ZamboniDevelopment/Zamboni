using System.Collections.Generic;
using System.Threading.Tasks;
using Blaze2SDK;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.Authentication;
using Blaze2SDK.Components;
using BlazeCommon;
using SceNetNp;

namespace Zamboni.Components.Blaze;

public class AuthenticationComponent : AuthenticationComponentBase.Server
{
    public override Task<ConsoleLoginResponse> Ps3LoginAsync(PS3LoginRequest request, BlazeRpcContext context)
    {
        if (!NpTicket.TryParse(request.mPS3Ticket, out NpTicket? ticket))
        {
            throw new BlazeRpcException(Blaze2RpcError.AUTH_ERR_INVALID_PS3_TICKET);
        }

        //Still unsure what EXBB is. Research concluded its
        //`externalblob` binary(36) DEFAULT NULL COMMENT 'sizeof(SceNpId)==36',
        //"SceNpId", Its 36 bytes long, it starts with PSN Username and suffixed with other data in the end
        //This taken straight from https://github.com/hallofmeat/Skateboard3Server/blob/master/src/Skateboard3Server.Blaze/Handlers/Authentication/LoginHandler.cs
        // var externalBlob = new List<byte>();
        // externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.OnlineId.PadRight(20, '\0')));
        // externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.Domain));
        // externalBlob.AddRange(Encoding.ASCII.GetBytes(ticket.Region));
        // externalBlob.AddRange(Encoding.ASCII.GetBytes("ps3"));
        // externalBlob.Add(0x0);
        // externalBlob.Add(0x1);
        // externalBlob.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var userIdentification = new UserIdentification
        {
            mAccountId = (long)ticket.SubjectId,
            mAccountLocale = 1701729619,
            // mExternalBlob = externalBlob.ToArray(),
            mExternalId = ticket.SubjectId,
            mBlazeId = (uint)ticket.SubjectId,
            mName = ticket.SubjectHandle,
            mPersonaId = ticket.SubjectHandle
        };

        var extendedData = new UserSessionExtendedData
        {
            mAddress = null!,
            mBestPingSiteAlias = "qos",
            mClientAttributes = new SortedDictionary<uint, int>(),
            mCountry = "",
            mDataMap = new SortedDictionary<uint, int>(),
            mHardwareFlags = HardwareFlags.None,
            mLatencyList = new List<int>
            {
                10
            },
            mQosData = default,
            mUserInfoAttribute = 0,
            mBlazeObjectIdList = new List<ulong>()
        };

        var sessionInfo = new SessionInfo
        {
            mBlazeUserId = (uint)ticket.SubjectId,
            mSessionKey = ticket.SubjectId.ToString(),
            mEmail = "",
            mPersonaDetails = new PersonaDetails
            {
                mDisplayName = ticket.SubjectHandle,
                mLastAuthenticated = 0,
                mPersonaId = (long)ticket.SubjectId,
                mExtId = ticket.SubjectId,
                mExtType = ExternalRefType.PS3
            },
            mUserId = (long)ticket.SubjectId
        };

        new ServerPlayer(context.BlazeConnection, userIdentification, extendedData, sessionInfo);

        Task.Run(async () =>
        {
            await Task.Delay(100);
            UserSessionsBase.Server.NotifyUserAddedAsync(context.BlazeConnection, userIdentification);
        });

        Task.Run(async () =>
        {
            await Task.Delay(200);
            UserSessionsBase.Server.NotifyUserSessionExtendedDataUpdateAsync(context.BlazeConnection,
                new UserSessionExtendedDataUpdate
                {
                    mExtendedData = extendedData,
                    mUserId = userIdentification.mBlazeId
                });
        });

        return Task.FromResult(new ConsoleLoginResponse
        {
            mSessionInfo = sessionInfo,
            mTosHost = "",
            mTosUri = ""
        });
    }

    public override Task<NullStruct> LogoutAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> CreateWalUserSessionAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}
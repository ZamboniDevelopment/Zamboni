using System.Collections.Generic;
using System.Threading.Tasks;
using Blaze2SDK.Blaze;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

public class UserSessionsComponent : UserSessionsBase.Server
{
    public override Task<NullStruct> UpdateNetworkInfoAsync(NetworkInfo request, BlazeRpcContext context)
    {
        var zamboniUser = Manager.GetZamboniUser(context.BlazeConnection);
        zamboniUser.NetworkInfo = request;

        NotifyUserSessionExtendedDataUpdateAsync(context.BlazeConnection,
            new UserSessionExtendedDataUpdate
            {
                mExtendedData = new UserSessionExtendedData
                {
                    mAddress = request.mAddress,
                    mBestPingSiteAlias = "qos",
                    mClientAttributes = new SortedDictionary<uint, int>(),
                    mCountry = "",
                    mDataMap = new SortedDictionary<uint, int>(),
                    mHardwareFlags = HardwareFlags.None,
                    mLatencyList = new List<int>
                    {
                        10
                    },
                    mQosData = request.mQosData,
                    mUserInfoAttribute = 0,
                    mBlazeObjectIdList = new List<ulong>()
                },
                mUserId = (uint)zamboniUser.UserId
            });

        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateHardwareFlagsAsync(UpdateHardwareFlagsRequest request,
        BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<UserData> LookupUserAsync(UserIdentification request, BlazeRpcContext context)
    {
        var target = Manager.GetZamboniUser(request.mName);

        if (target == null) return Task.FromResult(new UserData());

        return Task.FromResult(new UserData
        {
            mExtendedData = new UserSessionExtendedData
            {
                mAddress = target.NetworkInfo.mAddress,
                mBestPingSiteAlias = "qos",
                mClientAttributes = new SortedDictionary<uint, int>(),
                mDataMap = new SortedDictionary<uint, int>(),
                mHardwareFlags = HardwareFlags.None,
                mLatencyList = new List<int>(),
                mQosData = target.NetworkInfo.mQosData,
                mBlazeObjectIdList = new List<ulong>()
            },
            mStatusFlags = UserDataFlags.Online,
            mUserInfo = new UserIdentification
            {
                mAccountId = (long)target.UserId,
                mAccountLocale = 1701729619,
                mExternalBlob = new byte[]
                {
                },
                mExternalId = target.UserId,
                mBlazeId = (uint)target.UserId,
                mName = target.Username,
                mIsOnline = true,
                mPersonaId = target.Username
            }
        });
    }
}
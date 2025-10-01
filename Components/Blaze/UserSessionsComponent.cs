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
        var hockeyUser = Manager.GetHockeyUser(context.BlazeConnection);
        hockeyUser.NetworkAddress = request.mAddress;
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateHardwareFlagsAsync(UpdateHardwareFlagsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<UserData> LookupUserAsync(UserIdentification request, BlazeRpcContext context)
    {
        var target = Manager.GetHockeyUser(request.mName);

        if (target == null) return Task.FromResult(new UserData());

        return Task.FromResult(new UserData
        {
            mExtendedData = new UserSessionExtendedData
            {
                mAddress = target.NetworkAddress,
                mBestPingSiteAlias = "qos",
                mClientAttributes = new SortedDictionary<uint, int>(),
                mCountry = "country",
                mDataMap = new SortedDictionary<uint, int>(),
                mHardwareFlags = HardwareFlags.None,
                mLatencyList = new List<int>(),
                mQosData = default,
                mUserInfoAttribute = 0,
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
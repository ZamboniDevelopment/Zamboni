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
        var serverPlayer = ServerManager.GetServerPlayer(context.BlazeConnection);
        if (serverPlayer == null) return Task.FromResult(new NullStruct());
        var serverPlayerExtendedData = serverPlayer.ExtendedData;
        serverPlayerExtendedData.mAddress = request.mAddress;
        serverPlayerExtendedData.mQosData = request.mQosData;
        serverPlayerExtendedData.mBestPingSiteAlias = "qos";
        serverPlayer.ExtendedData = serverPlayerExtendedData;

        NotifyUserSessionExtendedDataUpdateAsync(serverPlayer.BlazeServerConnection,
            new UserSessionExtendedDataUpdate
            {
                mExtendedData = serverPlayerExtendedData,
                mUserId = serverPlayer.UserIdentification.mBlazeId
            });

        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> UpdateHardwareFlagsAsync(UpdateHardwareFlagsRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<UserData> LookupUserAsync(UserIdentification request, BlazeRpcContext context)
    {
        var target = ServerManager.GetServerPlayer(request.mName);

        if (target == null) return Task.FromResult(new UserData());

        return Task.FromResult(new UserData
        {
            mExtendedData = target.ExtendedData,
            mStatusFlags = UserDataFlags.Online,
            mUserInfo = target.UserIdentification
        });
    }
}
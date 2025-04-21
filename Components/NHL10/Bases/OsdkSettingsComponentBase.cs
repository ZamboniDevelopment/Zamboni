using System;
using System.Threading.Tasks;
using Blaze2SDK;
using Blaze2SDK.Blaze.Example;
using BlazeCommon;

namespace Zamboni.Components.NHL10.Bases
{
    public static class OsdkSettingsComponentBase
    {
        public const ushort Id = 2049;
        public const string Name = "OSDKSettingsComponent";

        public class Server : BlazeServerComponent<OsdkSettingsComponentCommand, OsdkSettingsComponentNotification, Blaze2RpcError>
        {
            public Server() : base(OsdkSettingsComponentBase.Id, OsdkSettingsComponentBase.Name) { }

            [BlazeCommand((ushort)OsdkSettingsComponentCommand.fetchSettings)]
            public virtual Task<NullStruct> fetchSettings(NullStruct request, BlazeRpcContext context)
            {
                throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
            }
            
            [BlazeCommand((ushort)OsdkSettingsComponentCommand.fetchSettingsGroups)]
            public virtual Task<NullStruct> fetchSettingsGroups(NullStruct request, BlazeRpcContext context)
            {
                throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
            }

            public override Type GetCommandRequestType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandRequestType(command);
            public override Type GetCommandResponseType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandResponseType(command);
            public override Type GetCommandErrorResponseType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandErrorResponseType(command);
            public override Type GetNotificationType(OsdkSettingsComponentNotification notification) => OsdkSettingsComponentBase.GetNotificationType(notification);
        }

        public static Type GetCommandRequestType(OsdkSettingsComponentCommand command) => command switch
        {
            OsdkSettingsComponentCommand.fetchSettings => typeof(NullStruct),
            OsdkSettingsComponentCommand.fetchSettingsGroups => typeof(NullStruct),
            _ => typeof(NullStruct)
        };

        public static Type GetCommandResponseType(OsdkSettingsComponentCommand command) => command switch
        {
            OsdkSettingsComponentCommand.fetchSettings => typeof(NullStruct),
            OsdkSettingsComponentCommand.fetchSettingsGroups => typeof(NullStruct),
            _ => typeof(NullStruct)
        };

        public static Type GetCommandErrorResponseType(OsdkSettingsComponentCommand command) => typeof(NullStruct);

        public static Type GetNotificationType(OsdkSettingsComponentNotification notification) => typeof(NullStruct);

        public enum OsdkSettingsComponentCommand : ushort
        {
            fetchSettings = 1,
            fetchSettingsGroups = 2
        }

        public enum OsdkSettingsComponentNotification : ushort
        {
            // Add as needed
        }
    }
}

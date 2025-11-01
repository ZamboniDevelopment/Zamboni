using System;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;
using NLog;
using Zamboni.Components.NHL10.Structs;

namespace Zamboni.Components.NHL10.Bases
{
    public static class OsdkSettingsComponentBase
    {
        public const ushort Id = 2049;
        public const string Name = "OSDKSettingsComponent";
        
        public class Server : BlazeServerComponent<OsdkSettingsComponentCommand, OsdkSettingsComponentNotification, Blaze2RpcError>
        {
            public Server() : base(OsdkSettingsComponentBase.Id, OsdkSettingsComponentBase.Name)
            {
                
            }
            
            [BlazeCommand((ushort)OsdkSettingsComponentCommand.fetchSettings)]
            public virtual Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request, BlazeRpcContext context)
            {
                throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
            }
            
            [BlazeCommand((ushort)OsdkSettingsComponentCommand.fetchSettingsGroups)]
            public virtual Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request, BlazeRpcContext context)
            {
                throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
            }
            public override Type GetCommandRequestType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandRequestType(command);
            public override Type GetCommandResponseType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandResponseType(command);
            public override Type GetCommandErrorResponseType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandErrorResponseType(command);
            public override Type GetNotificationType(OsdkSettingsComponentNotification notification) => OsdkSettingsComponentBase.GetNotificationType(notification);
            
        }
        
        public class Client : BlazeClientComponent<OsdkSettingsComponentCommand, OsdkSettingsComponentNotification, Blaze2RpcError>
        {
            BlazeClientConnection Connection { get; }
            private static Logger _logger = LogManager.GetCurrentClassLogger();
            
            public Client(BlazeClientConnection connection) : base(OsdkSettingsComponentBase.Id, OsdkSettingsComponentBase.Name)
            {
                Connection = connection;
                if (!Connection.Config.AddComponent(this))
                    throw new InvalidOperationException($"A component with Id({Id}) has already been created for the connection.");
            }
            
            
            public FetchSettingsResponse FetchSettings(NullStruct request)
            {
                return Connection.SendRequest<NullStruct, FetchSettingsResponse, NullStruct>(this, (ushort)OsdkSettingsComponentCommand.fetchSettings, request);
            }
            public Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request)
            {
                return Connection.SendRequestAsync<NullStruct, FetchSettingsResponse, NullStruct>(this, (ushort)OsdkSettingsComponentCommand.fetchSettings, request);
            }
            
            public FetchSettingsGroupsResponse FetchSettingsGroups(NullStruct request)
            {
                return Connection.SendRequest<NullStruct, FetchSettingsGroupsResponse, NullStruct>(this, (ushort)OsdkSettingsComponentCommand.fetchSettingsGroups, request);
            }
            public Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request)
            {
                return Connection.SendRequestAsync<NullStruct, FetchSettingsGroupsResponse, NullStruct>(this, (ushort)OsdkSettingsComponentCommand.fetchSettingsGroups, request);
            }
            
            public override Type GetCommandRequestType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandRequestType(command);
            public override Type GetCommandResponseType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandResponseType(command);
            public override Type GetCommandErrorResponseType(OsdkSettingsComponentCommand command) => OsdkSettingsComponentBase.GetCommandErrorResponseType(command);
            public override Type GetNotificationType(OsdkSettingsComponentNotification notification) => OsdkSettingsComponentBase.GetNotificationType(notification);
            
        }
        
        public class Proxy : BlazeProxyComponent<OsdkSettingsComponentCommand, OsdkSettingsComponentNotification, Blaze2RpcError>
        {
            public Proxy() : base(OsdkSettingsComponentBase.Id, OsdkSettingsComponentBase.Name)
            {
                
            }
            
            [BlazeCommand((ushort)OsdkSettingsComponentCommand.fetchSettings)]
            public virtual Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request, BlazeProxyContext context)
            {
                return context.ClientConnection.SendRequestAsync<NullStruct, FetchSettingsResponse, NullStruct>(this, (ushort)OsdkSettingsComponentCommand.fetchSettings, request);
            }
            
            [BlazeCommand((ushort)OsdkSettingsComponentCommand.fetchSettingsGroups)]
            public virtual Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request, BlazeProxyContext context)
            {
                return context.ClientConnection.SendRequestAsync<NullStruct, FetchSettingsGroupsResponse, NullStruct>(this, (ushort)OsdkSettingsComponentCommand.fetchSettingsGroups, request);
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
            OsdkSettingsComponentCommand.fetchSettings => typeof(FetchSettingsResponse),
            OsdkSettingsComponentCommand.fetchSettingsGroups => typeof(FetchSettingsGroupsResponse),
            _ => typeof(NullStruct)
        };
        
        public static Type GetCommandErrorResponseType(OsdkSettingsComponentCommand command) => command switch
        {
            OsdkSettingsComponentCommand.fetchSettings => typeof(NullStruct),
            OsdkSettingsComponentCommand.fetchSettingsGroups => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
        
        public static Type GetNotificationType(OsdkSettingsComponentNotification notification) => notification switch
        {
            _ => typeof(NullStruct)
        };
        
        public enum OsdkSettingsComponentCommand : ushort
        {
            fetchSettings = 1,
            fetchSettingsGroups = 2,
        }
        
        public enum OsdkSettingsComponentNotification : ushort
        {
        }
        
    }
}

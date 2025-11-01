using System;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;
using NLog;
using Zamboni.Components.NHL10.Structs;

namespace Zamboni.Components.NHL10.Bases
{
    public static class DynamicMessagingComponentBase
    {
        public const ushort Id = 69;
        public const string Name = "DynamicMessagingComponent";
        
        public class Server : BlazeServerComponent<DynamicMessagingComponentComponentCommand, DynamicMessagingComponentNotification, Blaze2RpcError>
        {
            public Server() : base(DynamicMessagingComponentBase.Id, DynamicMessagingComponentBase.Name)
            {
                
            }
            
            [BlazeCommand((ushort)DynamicMessagingComponentComponentCommand.getMessages)]
            public virtual Task<NullStruct> GetMessagesAsync(NullStruct request, BlazeRpcContext context)
            {
                throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
            }
            
            [BlazeCommand((ushort)DynamicMessagingComponentComponentCommand.getConfig)]
            public virtual Task<DynamicConfigResponse> GetDynamicConfigAsync(NullStruct request, BlazeRpcContext context)
            {
                throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
            }
            public override Type GetCommandRequestType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandRequestType(componentCommand);
            public override Type GetCommandResponseType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandResponseType(componentCommand);
            public override Type GetCommandErrorResponseType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandErrorResponseType(componentCommand);
            public override Type GetNotificationType(DynamicMessagingComponentNotification notification) => DynamicMessagingComponentBase.GetNotificationType(notification);
            
        }
        
        public class Client : BlazeClientComponent<DynamicMessagingComponentComponentCommand, DynamicMessagingComponentNotification, Blaze2RpcError>
        {
            BlazeClientConnection Connection { get; }
            private static Logger _logger = LogManager.GetCurrentClassLogger();
            
            public Client(BlazeClientConnection connection) : base(DynamicMessagingComponentBase.Id, DynamicMessagingComponentBase.Name)
            {
                Connection = connection;
                if (!Connection.Config.AddComponent(this))
                    throw new InvalidOperationException($"A component with Id({Id}) has already been created for the connection.");
            }
            
            public NullStruct GetMessages(NullStruct request)
            {
                return Connection.SendRequest<NullStruct, NullStruct, NullStruct>(this, (ushort)DynamicMessagingComponentComponentCommand.getMessages, request);
            }
            public Task<NullStruct> GetMessagesAsync(NullStruct request)
            {
                return Connection.SendRequestAsync<NullStruct, NullStruct, NullStruct>(this, (ushort)DynamicMessagingComponentComponentCommand.getMessages, request);
            }
            
            public DynamicConfigResponse GetConfig(NullStruct request)
            {
                return Connection.SendRequest<NullStruct, DynamicConfigResponse, NullStruct>(this, (ushort)DynamicMessagingComponentComponentCommand.getConfig, request);
            }
            public Task<DynamicConfigResponse> GetConfigAsync(NullStruct request)
            {
                return Connection.SendRequestAsync<NullStruct, DynamicConfigResponse, NullStruct>(this, (ushort)DynamicMessagingComponentComponentCommand.getConfig, request);
            }

            public override Type GetCommandRequestType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandRequestType(componentCommand);
            public override Type GetCommandResponseType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandResponseType(componentCommand);
            public override Type GetCommandErrorResponseType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandErrorResponseType(componentCommand);
            public override Type GetNotificationType(DynamicMessagingComponentNotification notification) => DynamicMessagingComponentBase.GetNotificationType(notification);
            
        }
        
        public class Proxy : BlazeProxyComponent<DynamicMessagingComponentComponentCommand, DynamicMessagingComponentNotification, Blaze2RpcError>
        {
            public Proxy() : base(DynamicMessagingComponentBase.Id, DynamicMessagingComponentBase.Name)
            {
                
            }
            
            [BlazeCommand((ushort)DynamicMessagingComponentComponentCommand.getMessages)]
            public virtual Task<NullStruct> GetMessagesAsync(NullStruct request, BlazeProxyContext context)
            {
                return context.ClientConnection.SendRequestAsync<NullStruct, NullStruct, NullStruct>(this, (ushort)DynamicMessagingComponentComponentCommand.getMessages, request);
            }
            
            [BlazeCommand((ushort)DynamicMessagingComponentComponentCommand.getConfig)]
            public virtual Task<DynamicConfigResponse> GetConfigAsync(NullStruct request, BlazeProxyContext context)
            {
                return context.ClientConnection.SendRequestAsync<NullStruct, DynamicConfigResponse, NullStruct>(this, (ushort)DynamicMessagingComponentComponentCommand.getConfig, request);
            }
            
            
            public override Type GetCommandRequestType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandRequestType(componentCommand);
            public override Type GetCommandResponseType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandResponseType(componentCommand);
            public override Type GetCommandErrorResponseType(DynamicMessagingComponentComponentCommand componentCommand) => DynamicMessagingComponentBase.GetCommandErrorResponseType(componentCommand);
            public override Type GetNotificationType(DynamicMessagingComponentNotification notification) => DynamicMessagingComponentBase.GetNotificationType(notification);
            
        }
        
        public static Type GetCommandRequestType(DynamicMessagingComponentComponentCommand componentCommand) => componentCommand switch
        {
            DynamicMessagingComponentComponentCommand.getMessages => typeof(NullStruct),
            DynamicMessagingComponentComponentCommand.getConfig => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
        
        public static Type GetCommandResponseType(DynamicMessagingComponentComponentCommand componentCommand) => componentCommand switch
        {
            DynamicMessagingComponentComponentCommand.getMessages => typeof(NullStruct),
            DynamicMessagingComponentComponentCommand.getConfig => typeof(DynamicConfigResponse),
            _ => typeof(NullStruct)
        };
        
        public static Type GetCommandErrorResponseType(DynamicMessagingComponentComponentCommand componentCommand) => componentCommand switch
        {
            DynamicMessagingComponentComponentCommand.getMessages => typeof(NullStruct),
            DynamicMessagingComponentComponentCommand.getConfig => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
        
        public static Type GetNotificationType(DynamicMessagingComponentNotification notification) => notification switch
        {
            _ => typeof(NullStruct)
        };
        
        public enum DynamicMessagingComponentComponentCommand : ushort
        {
            getMessages = 1,
            getConfig = 2,
        }
        
        public enum DynamicMessagingComponentNotification : ushort
        {
        }
        
    }
}

using System;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;
using NLog;
using Zamboni.Components.NHL10.Structs;

namespace Zamboni.Components.NHL10.Bases;

public static class OSDKSettingsComponentBase
{
    public enum OSDKSettingsComponentCommand : ushort
    {
        fetchSettings = 1,
        fetchSettingsGroups = 2
    }

    public enum OSDKSettingsComponentNotification : ushort
    {
    }

    public const ushort Id = 2049;
    public const string Name = "OSDKSettingsComponent";

    public static Type GetCommandRequestType(OSDKSettingsComponentCommand componentCommand)
    {
        return componentCommand switch
        {
            OSDKSettingsComponentCommand.fetchSettings => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
    }

    public static Type GetCommandResponseType(OSDKSettingsComponentCommand componentCommand)
    {
        return componentCommand switch
        {
            OSDKSettingsComponentCommand.fetchSettings => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
    }

    public static Type GetCommandErrorResponseType(OSDKSettingsComponentCommand componentCommand)
    {
        return componentCommand switch
        {
            OSDKSettingsComponentCommand.fetchSettings => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
    }

    public static Type GetNotificationType(OSDKSettingsComponentNotification notification)
    {
        return notification switch
        {
            _ => typeof(NullStruct)
        };
    }

    public class Server : BlazeServerComponent<OSDKSettingsComponentCommand, OSDKSettingsComponentNotification, Blaze2RpcError>
    {
        public Server() : base(OSDKSettingsComponentBase.Id, OSDKSettingsComponentBase.Name)
        {
        }

        [BlazeCommand((ushort)OSDKSettingsComponentCommand.fetchSettings)]
        public virtual Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request, BlazeRpcContext context)
        {
            throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
        }

        [BlazeCommand((ushort)OSDKSettingsComponentCommand.fetchSettingsGroups)]
        public virtual Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request, BlazeRpcContext context)
        {
            throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
        }

        public override Type GetCommandRequestType(OSDKSettingsComponentCommand componentCommand)
        {
            return OSDKSettingsComponentBase.GetCommandRequestType(componentCommand);
        }

        public override Type GetCommandResponseType(OSDKSettingsComponentCommand componentCommand)
        {
            return OSDKSettingsComponentBase.GetCommandResponseType(componentCommand);
        }

        public override Type GetCommandErrorResponseType(OSDKSettingsComponentCommand componentCommand)
        {
            return OSDKSettingsComponentBase.GetCommandErrorResponseType(componentCommand);
        }

        public override Type GetNotificationType(OSDKSettingsComponentNotification notification)
        {
            return OSDKSettingsComponentBase.GetNotificationType(notification);
        }
    }

    public class Client : BlazeClientComponent<OSDKSettingsComponentCommand, OSDKSettingsComponentNotification, Blaze2RpcError>
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public Client(BlazeClientConnection connection) : base(OSDKSettingsComponentBase.Id, OSDKSettingsComponentBase.Name)
        {
            Connection = connection;
            if (!Connection.Config.AddComponent(this))
                throw new InvalidOperationException($"A component with Id({Id}) has already been created for the connection.");
        }

        private BlazeClientConnection Connection { get; }

        public FetchSettingsResponse FetchSettings(NullStruct request)
        {
            return Connection.SendRequest<NullStruct, FetchSettingsResponse, NullStruct>(this, (ushort)OSDKSettingsComponentCommand.fetchSettings, request);
        }

        public Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request)
        {
            return Connection.SendRequestAsync<NullStruct, FetchSettingsResponse, NullStruct>(this, (ushort)OSDKSettingsComponentCommand.fetchSettings, request);
        }

        public FetchSettingsGroupsResponse FetchSettingsGroups(NullStruct request)
        {
            return Connection.SendRequest<NullStruct, FetchSettingsGroupsResponse, NullStruct>(this, (ushort)OSDKSettingsComponentCommand.fetchSettingsGroups, request);
        }

        public Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request)
        {
            return Connection.SendRequestAsync<NullStruct, FetchSettingsGroupsResponse, NullStruct>(this, (ushort)OSDKSettingsComponentCommand.fetchSettingsGroups, request);
        }

        public override Type GetCommandRequestType(OSDKSettingsComponentCommand componentCommand)
        {
            return OSDKSettingsComponentBase.GetCommandRequestType(componentCommand);
        }

        public override Type GetCommandResponseType(OSDKSettingsComponentCommand componentCommand)
        {
            return OSDKSettingsComponentBase.GetCommandResponseType(componentCommand);
        }

        public override Type GetCommandErrorResponseType(OSDKSettingsComponentCommand componentCommand)
        {
            return OSDKSettingsComponentBase.GetCommandErrorResponseType(componentCommand);
        }

        public override Type GetNotificationType(OSDKSettingsComponentNotification notification)
        {
            return OSDKSettingsComponentBase.GetNotificationType(notification);
        }
    }
    
}
using System;
using System.Threading.Tasks;
using Blaze2SDK;
using BlazeCommon;

namespace Zamboni.Components.NHL10.Bases;

public static class DynamicMessagingComponentBase
{
    public enum DynamicMessagingComponentCommand : ushort
    {
        getMessages = 1,
        getConfig = 2
    }

    public enum DynamicMessagingComponentNotification : ushort
    {
        // Add as needed
    }

    public const ushort Id = 69;
    public const string Name = "DynamicMessagingComponent";

    public static Type GetCommandRequestType(DynamicMessagingComponentCommand command)
    {
        return command switch
        {
            DynamicMessagingComponentCommand.getMessages => typeof(NullStruct),
            DynamicMessagingComponentCommand.getConfig => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
    }

    public static Type GetCommandResponseType(DynamicMessagingComponentCommand command)
    {
        return command switch
        {
            DynamicMessagingComponentCommand.getMessages => typeof(NullStruct),
            DynamicMessagingComponentCommand.getConfig => typeof(NullStruct),
            _ => typeof(NullStruct)
        };
    }

    public static Type GetCommandErrorResponseType(DynamicMessagingComponentCommand command)
    {
        return typeof(NullStruct);
    }

    public static Type GetNotificationType(DynamicMessagingComponentNotification notification)
    {
        return typeof(NullStruct);
    }

    public class Server : BlazeServerComponent<DynamicMessagingComponentCommand,
        DynamicMessagingComponentNotification, Blaze2RpcError>
    {
        public Server() : base(DynamicMessagingComponentBase.Id, DynamicMessagingComponentBase.Name)
        {
        }


        [BlazeCommand((ushort)DynamicMessagingComponentCommand.getMessages)]
        public virtual Task<NullStruct> getMessages(NullStruct request, BlazeRpcContext context)
        {
            throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
        }

        [BlazeCommand((ushort)DynamicMessagingComponentCommand.getConfig)]
        public virtual Task<NullStruct> getConfig(NullStruct request, BlazeRpcContext context)
        {
            throw new BlazeRpcException(Blaze2RpcError.ERR_COMMAND_NOT_FOUND);
        }


        public override Type GetCommandRequestType(DynamicMessagingComponentCommand command)
        {
            return DynamicMessagingComponentBase.GetCommandRequestType(command);
        }

        public override Type GetCommandResponseType(DynamicMessagingComponentCommand command)
        {
            return DynamicMessagingComponentBase.GetCommandResponseType(command);
        }

        public override Type GetCommandErrorResponseType(DynamicMessagingComponentCommand command)
        {
            return DynamicMessagingComponentBase.GetCommandErrorResponseType(command);
        }

        public override Type GetNotificationType(DynamicMessagingComponentNotification notification)
        {
            return DynamicMessagingComponentBase.GetNotificationType(notification);
        }
    }
}
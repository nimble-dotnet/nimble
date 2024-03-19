using System;
using Piot.Flood;

namespace Piot.Relay
{
    public class RelayWriter
    {
        public static void WriteRequestConnectToServer(IOctetWriter writer, UserSessionId relayUserSessionId,
            ConnectRequest request)
        {
            WriteCommand(writer, Command.CmdConnectRequestToServer);

            WriteUserSessionId(writer, relayUserSessionId);
            WriteUserId(writer, request.connectToUserId);
            WriteApplicationId(writer, request.appId);
            WriteChannelId(writer, request.channelId);
            WriteRequestId(writer, request.requestId);
        }

        private static void WriteRequestId(IOctetWriter writer, RequestId requestRequestId)
        {
            writer.WriteUInt64(requestRequestId.value);
        }
        private static void WriteChannelId(IOctetWriter writer, ChannelId requestChannelId)
        {
            writer.WriteUInt16(requestChannelId.value);
        }
        private static void WriteApplicationId(IOctetWriter writer, ApplicationId requestAppId)
        {
            writer.WriteUInt64(requestAppId.value);
        }

        private static void WriteUserId(IOctetWriter writer, UserId requestConnectToUserId)
        {
            writer.WriteUInt64(requestConnectToUserId.value);
        }

        private static void WriteUserSessionId(IOctetWriter writer, UserSessionId relayUserSessionId)
        {
            writer.WriteUInt64(relayUserSessionId.value);
        }

        private static void WriteCommand(IOctetWriter writer, Command command)
        {
            writer.WriteUInt8((byte)command);
        }

        public static void RequestListen(IOctetWriter writer, UserSessionId relayUserSessionId,
            ListenRequest listenRequest)
        {
            WriteCommand(writer, Command.CmdListenRequestToServer);
            WriteUserSessionId(writer, relayUserSessionId);
            WriteApplicationId(writer, listenRequest.appId);
            WriteChannelId(writer, listenRequest.channelId);
            WriteRequestId(writer, listenRequest.requestId);
        }

        private static void WriteConnectionId(IOctetWriter writer, ConnectionId connectionId)
        {
            writer.WriteUInt64(connectionId.value);
        }

        public static void SendPacketToServer(IOctetWriter writer, UserSessionId userSessionId,
            ConnectionId connectionId, ReadOnlySpan<byte> payload)
        {
            WriteCommand(writer, Command.CmdPacket);
            WriteUserSessionId(writer, userSessionId);
            WriteConnectionId(writer, connectionId);
            writer.WriteUInt16((ushort)payload.Length);
            writer.WriteOctets(payload);
        }
    }
}
using System;
using Piot.Flood;

namespace Piot.Relay
{
    /*
    public class RelayReader
    {
        ConnectRequestFromServerToListener ReadConnectRequestToListener(IOctetReader reader)
        {
            return new ConnectRequestFromServerToListener()
            {
                listenerId = ReadListenerId(reader),
                connectionId = ReadConnectionId(reader),
                userId = ReadUserId(reader),
                debugRequestId = ReadRequestId(reader),
            };
        }

        ReadOnlySpan<byte> ReadPacketFromServer(IOctetReader reader, out ConnectionId connectionId)
        {
            connectionId = ReadConnectionId(reader);
            var length = reader.ReadUInt16();
            return reader.ReadOctets(length);
        }

        ListenResponseFromServer ReadListenResponse(IOctetReader reader)
        {
            return new ListenResponseFromServer()
            {
                listenerId = ReadListenerId(reader),
                appId = ReadApplicationId(reader),
                channelId = ReadChannelId(reader),
                requestId = ReadRequestId(reader),
            };
        }

        public static ConnectResponseFromServer ReadConnectResponse(IOctetReader reader)
        {
            ReadConnectionId(inStream, &data->assignedConnectionId);
            return ReadRequestId(inStream, &data->requestId);
        }

        int PacketHeader(struct FldInStream* inStream, ServerPacketFromServerToClient* data)
        {
            ReadConnectionId(inStream, &data->connectionId);
            return fldInStreamReadUInt16(inStream, &data->packetOctetCount);
        }
    }
    */
}
namespace Piot.Relay
{
    public struct UserSessionId
    {
        public ulong value;
    }

    public struct ApplicationId
    {
        public ulong value;
    }

    public struct ChannelId
    {
        public ushort value;
    }

    public struct UserId
    {
        public ulong value;
    }

    public struct RequestId
    {
        public byte value;
    }

    public enum Command : byte
    {
        CmdPacket = 0x05,
        CmdConnectRequestToServer = 0x06,
        CmdListenRequestToServer = 0x07,
        CmdPacketToClient = 0x24,
        CmdConnectionRequestToClient = 0x25,
        CmdListenResponseToClient = 0x26,
        CmdConnectResponseToClient = 0x27,
    }

    public struct ConnectRequest
    {
        public ApplicationId appId;
        public ChannelId channelId;
        public UserId connectToUserId;
        public RequestId requestId;
    }
    
    public struct ListenRequest
    {
        public ApplicationId appId;
        public ChannelId channelId;
        public RequestId requestId;
    }
    
    public struct ConnectionId
    {
        public ulong value;
    }
}
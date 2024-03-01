using System;

namespace Piot.Datagram
{
    public struct HostDatagram
    {
        public byte connection;
        public Memory<byte> payload;
    }
}

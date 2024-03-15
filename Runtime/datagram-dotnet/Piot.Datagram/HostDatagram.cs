using System;

namespace Piot.Datagram
{
    public struct HostDatagram
    {
        public byte connection;
        public byte[] payload;

        public override string ToString()
        {
            return $"[HostDatagram connection:{connection} octetSize:{payload.Length}]";
        }
    }
}

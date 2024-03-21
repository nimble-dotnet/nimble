using System;

namespace Piot.Datagram
{
    /// <summary>
    /// Represents a datagram from or to a specific connection.
    /// </summary>
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

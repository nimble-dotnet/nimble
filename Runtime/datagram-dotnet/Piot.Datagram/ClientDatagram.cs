using System;

namespace Piot.Datagram
{
    /// <summary>
    /// Represents a datagram to or from a client, with a payload up to 1200 octets.
    /// </summary>
    public struct ClientDatagram
    {
        public byte[] payload;
    }

}

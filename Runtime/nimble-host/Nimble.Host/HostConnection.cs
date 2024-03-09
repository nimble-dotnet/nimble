using Piot.MonotonicTimeLowerBits;

namespace Nimble.Authoritative.Steps
{
    public class HostConnection
    {
        public ConnectionToParticipants connectionToParticipants = new();

        private byte connectionId;
        public MonotonicTimeLowerBits lastReceivedMonotonicLowerBits;

        public HostConnection(byte connectionId)
        {
            this.connectionId = connectionId;
        }

        public override string ToString()
        {
            return $"[HostConnection {connectionId} participantCount:{connectionToParticipants.connections.Count}]";
        }
    }
}
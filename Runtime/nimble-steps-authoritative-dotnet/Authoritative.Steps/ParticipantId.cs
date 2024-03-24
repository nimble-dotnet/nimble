namespace Piot.Nimble.Authoritative.Steps
{
    public struct ParticipantId
    {
        public byte id;


        public ParticipantId(byte id)
        {
            this.id = id;
        }

        public override string ToString()
        {
            return $"[participantId {id}]";
        }
    }
}
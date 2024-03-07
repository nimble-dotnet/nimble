using System;
using System.Collections.Generic;
using Piot.Nimble.Steps;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
    public class Participants
    {
        public readonly Dictionary<byte, Participant> participants = new();

        public Participant CreateParticipant(byte connectionId, LocalPlayerIndex localPlayerIndex)
        {
            var newParticipantId = GetFreeParticipantId();
            var newConnection = new Participant(newParticipantId, localPlayerIndex);

            participants.Add(newParticipantId.id, newConnection);

            return newConnection;
        }

        public void RemoveParticipant(Participant participant)
        {
            participants.Remove(participant.participantId.id);
        }

        public Participant GetParticipant(ParticipantId participantId)
        {
            return participants[participantId.id];
        }


        public ParticipantId GetFreeParticipantId()
        {
            for (var i = (byte)0; i < 64; ++i)
            {
                if (!participants.ContainsKey(i))
                {
                    // HACK:

                    if (i > 3)
                    {
                        throw new Exception($"only 0-3 are valid numbers in this nimble version {i}");
                    }

                    return new ParticipantId(i);
                }
            }

            throw new Exception("out of participant Ids");
        }

        public bool IsAnyoneAheadOfTheRequestedTickId(TickId tickId)
        {
            if (participants.Count == 0)
            {
                return false;
            }

            var maxCount = 0u;
            var connectionCountThatCouldNotContribute = 0u;

            foreach (var participant in participants.Values)
            {
                if (participant.incomingSteps.IsEmpty)
                {
                    connectionCountThatCouldNotContribute++;
                    continue;
                }

                // Is there a gap
                if (participant.incomingSteps.Peek().appliedAtTickId > tickId)
                {
                    connectionCountThatCouldNotContribute++;
                    continue;
                }

                var delta = (long)participant.incomingSteps.Last.appliedAtTickId.tickId - (long)tickId.tickId;
                if (delta > maxCount)
                {
                    maxCount = (uint)delta;
                }
            }

            switch (maxCount)
            {
                case > 15: // It is so much ahead, that we simply must do it
                    return true;

                case > 5:
                    return connectionCountThatCouldNotContribute == 0;
            }


            return false;
        }
    }
}
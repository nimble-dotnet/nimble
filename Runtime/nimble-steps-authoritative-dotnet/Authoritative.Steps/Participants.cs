using System;
using System.Collections.Generic;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
    public class Participants
    {
        const uint MaxParticipants = 32;

        public readonly Dictionary<byte, Participant> participants = new();
        private readonly ILog log;

        public Participants(ILog log)
        {
            this.log = log;
        }
        public Participant CreateParticipant(byte connectionId, LocalPlayerIndex localPlayerIndex)
        {
            var newParticipantId = GetFreeParticipantId();
            var newConnection = new Participant(newParticipantId, localPlayerIndex);
            log.Debug("add participant {ParticipantID} for {ConnectionID} {LocalPlayerIndex}", newParticipantId, connectionId, localPlayerIndex);

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
            for (var i = (byte)0; i < MaxParticipants; ++i)
            {
                if (!participants.ContainsKey(i))
                {
                    return new ParticipantId(i);
                }
            }

            throw new Exception($"out of participant Ids {participants.Count}/{MaxParticipants}");
        }

        public bool IsAnyoneAheadOfTheRequestedTickId(TickId tickId)
        {
            if (participants.Count == 0)
            {
                //log.Warn("no participants at all");
                return false;
            }

            var maxCount = 0u;
            var connectionCountThatCouldNotContribute = 0u;

            foreach (var participant in participants.Values)
            {
                if (participant.incomingSteps.IsEmpty)
                {
                    //                  log.Warn("{Participant} could not contribute, buffer is empty", participant);

                    connectionCountThatCouldNotContribute++;
                    continue;
                }

                // Is there a gap
                if (participant.incomingSteps.Peek().appliedAtTickId > tickId)
                {
                    //                    log.Warn("{Participant} first step is at a gap, {TickID}. Wanted to compose {ComposeTickID}", participant, participant.incomingSteps.Peek().appliedAtTickId, tickId);
                    //connectionCountThatCouldNotContribute++;
                }

                var stepCountThatCanBeContributed = (long)participant.incomingSteps.Last.appliedAtTickId.tickId - (long)tickId.tickId + 1;
                if (stepCountThatCanBeContributed > maxCount)
                {
                    //              log.Warn("{Participant} is the best contributor with {Delta} steps", participant, delta);
                    maxCount = (uint)stepCountThatCanBeContributed;
                }
                else
                {
                    //                log.Warn("{Participant} was not a great contributor {Delta}. {First} {Last}", participant, delta, participant.incomingSteps.Peek().appliedAtTickId,  participant.incomingSteps.Last.appliedAtTickId);
                }
            }
            
           // log.DebugLowLevel("IsAhead {MaxCount} {ConnectionCountThatCouldNotContribute}", maxCount, connectionCountThatCouldNotContribute);

            switch (maxCount)
            {
                case >= 4:
                    return connectionCountThatCouldNotContribute <= participants.Count / 2;

                case >= 1:
                    //                    log.Warn("{CountThatCouldNotContribute}", connectionCountThatCouldNotContribute);
                    return connectionCountThatCouldNotContribute == 0;
            }


            return false;
        }
    }
}
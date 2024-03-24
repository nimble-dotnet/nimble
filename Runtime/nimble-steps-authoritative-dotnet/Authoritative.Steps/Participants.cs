using System;
using System.Collections.Generic;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Tick;

namespace Piot.Nimble.Authoritative.Steps
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
            log.Debug("add participant {ParticipantID} for {ConnectionID} {LocalPlayerIndex}", newParticipantId,
                connectionId, localPlayerIndex);

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

            var couldAnyoneContributeNow = false;
            foreach (var participant in participants.Values)
            {
                if (participant.incomingSteps.IsEmpty)
                {
                    //                  log.Warn("{Participant} could not contribute, buffer is empty", participant);

                    continue;
                }

                if (participant.incomingSteps.Peek().appliedAtTickId == tickId && participant.incomingSteps.Count >= 3)
                {
                    log.DebugLowLevel("We have at least one {Participant} that can contribute now and {Delta} steps",
                        participant, participant.incomingSteps.Count);
                    couldAnyoneContributeNow = true;
                    break;
                }
                else
                {
                    log.DebugLowLevel("We have at least one {Participant} that CAN NOT {TickId} {Range}", participant,
                        tickId, participant.incomingSteps.Range);
                }
            }

            if (!couldAnyoneContributeNow)
            {
                return false;
            }

            var couldEveryoneContributeNowOrInTheFuture = true;

            // Someone can contribute to the current step, add penalty for anyone that can not contribute now or at least in the future
            foreach (var participant in participants.Values)
            {
                if (participant.incomingSteps.IsEmpty || participant.incomingSteps.Last.appliedAtTickId < tickId)
                {
                    couldEveryoneContributeNowOrInTheFuture = false;
                    participant.AddPenalty();
                    log.DebugLowLevel("{Participant} can not contribute now or in the future", participant);
                    if (participant.Penalty > 50)
                    {
                        log.Warn("should disconnect {Participant}", participant);
                    }
                }
                else
                {
                    participant.ClearPenalty();
                }
            }

            log.DebugLowLevel("IsAhead {AllowedToAdvance}", couldEveryoneContributeNowOrInTheFuture);

            return couldEveryoneContributeNowOrInTheFuture;
        }
    }
}
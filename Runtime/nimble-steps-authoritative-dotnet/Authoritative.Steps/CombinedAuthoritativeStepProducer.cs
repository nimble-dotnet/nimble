using Piot.Clog;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
    public class CombinedAuthoritativeStepProducer
    {
        private Participants participants;
        private CombinedAuthoritativeStepsQueue combinedAuthoritativeStepsQueue;
        private TickId tickId;
        private ILog log;

        public CombinedAuthoritativeStepsQueue AuthoritativeStepsQueue => combinedAuthoritativeStepsQueue;

        public CombinedAuthoritativeStepProducer(TickId tickId, Participants participants, ILog log)
        {
            this.tickId = tickId;
            this.log = log;
            this.participants = participants;
            combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
        }

        private CombinedAuthoritativeStep ComposeOneStep()
        {
            var combinedAuthoritativeStep = Combiner.ComposeOneAuthoritativeSteps(participants, tickId, log);
            tickId = tickId.Next;

            combinedAuthoritativeStepsQueue.Add(combinedAuthoritativeStep);

            return combinedAuthoritativeStep;
        }

        private bool TryToComposeOneStep()
        {
            var isAnyoneAhead = participants.IsAnyoneAheadOfTheRequestedTickId(tickId);
            if (!isAnyoneAhead)
            {
                log.Warn("no connection is ahead, can not produce authoritative input {TickID}", tickId);
                foreach (var (_, participant) in participants.participants)
                {
                    if(participant.incomingSteps.IsEmpty)
                    {
                        continue;
                    }
                    log.Warn("{Participant} is at {TickIdRange}", participant, participant.incomingSteps.Range);
                }
                return false;
            }
            
            foreach (var (_, participant) in participants.participants)
            {
                if(participant.incomingSteps.IsEmpty)
                {
                    continue;
                }
                log.Warn("..Composing a step: {TickID} {Participant} is at {TickIdRange}", tickId, participant, participant.incomingSteps.Range);
            }
            ComposeOneStep();

            return true;
        }

        public void Tick()
        {
            var stepsComposedCount = 0u;
            while (TryToComposeOneStep())
            {
                stepsComposedCount++;
            }
            
//            log.Warn("steps composed in one tick {StepsComposedCount}", stepsComposedCount);
        }
    }
}
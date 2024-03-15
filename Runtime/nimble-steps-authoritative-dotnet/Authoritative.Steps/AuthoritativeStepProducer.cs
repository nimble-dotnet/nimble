using Piot.Clog;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
    /// <summary>
    /// Created combined authoritative steps from participants incoming predicted steps.
    /// </summary>
    public class AuthoritativeStepProducer
    {
        private Participants participants;
        private AuthoritativeStepsQueue _authoritativeStepsQueue;
        private TickId tickId;
        private ILog log;

        public AuthoritativeStepsQueue AuthoritativeStepsQueue => _authoritativeStepsQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthoritativeStepProducer"/> class.
        /// </summary>
        /// <param name="tickId">The initial tick ID to be produced.</param>
        /// <param name="participants">The participants.</param>
        /// <param name="log">The log.</param>
        public AuthoritativeStepProducer(TickId tickId, Participants participants, ILog log)
        {
            this.tickId = tickId;
            this.log = log;
            this.participants = participants;
            _authoritativeStepsQueue = new AuthoritativeStepsQueue(tickId);
        }

        /// <summary>
        /// Composes one combined authoritative step.
        /// </summary>
        /// <remarks>
        /// It will always be able to produce a step. If a participant can not provide a predicted step for the current tickId, one will be
        /// created anyway and marked with SerializeProviderConnectState.StepNotProvidedInTime.
        /// </remarks>
        private void ComposeOneStep()
        {
            var combinedAuthoritativeStep = Combiner.ComposeOneAuthoritativeSteps(participants, tickId, log);
            tickId = tickId.Next;

            _authoritativeStepsQueue.Add(combinedAuthoritativeStep);
        }

        /// <summary>
        /// Tries to compose one combined authoritative step. If not all connections have steps to provide, it will return false.
        /// </summary>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        private bool TryToComposeOneStep()
        {
            var isAnyoneAhead = participants.IsAnyoneAheadOfTheRequestedTickId(tickId);
            if (!isAnyoneAhead)
            {
                /*
                log.DebugLowLevel("no connection is ahead, can not produce authoritative input {TickID}", tickId);

                foreach (var (_, participant) in participants.participants)
                {
                    if(participant.incomingSteps.IsEmpty)
                    {
                        log.DebugLowLevel("{Participant} is empty {TickID}", participant, participant.incomingSteps.WaitingForTickId);
                        continue;
                    }
                    log.DebugLowLevel("{Participant} is at {TickIdRange}", participant, participant.incomingSteps.Range);
                }
                */
                return false;
            }
            
            /*
            foreach (var (_, participant) in participants.participants)
            {
                if(participant.incomingSteps.IsEmpty)
                {
                    continue;
                }
                log.DebugLowLevel("..Composing a step: {TickID} {Participant} is at {TickIdRange}", tickId, participant, participant.incomingSteps.Range);
            }
            */
            ComposeOneStep();

            return true;
        }

        /// <summary>
        /// Produces as many authoritative steps as possible
        /// </summary>
        public void Tick()
        {
            //var stepsComposedCount = 0u;
            while (TryToComposeOneStep())
            {
              //  stepsComposedCount++;
            }
            
//            log.Warn("steps composed in one tick {StepsComposedCount}", stepsComposedCount);
        }
    }
}
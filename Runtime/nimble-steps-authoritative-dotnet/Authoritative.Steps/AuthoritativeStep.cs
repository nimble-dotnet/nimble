using System.Collections.Generic;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
    public class AuthoritativeStep
    {
        public readonly TickId appliedAtTickId;
        public readonly Dictionary<ParticipantId, AuthoritativeStepForOneParticipant> authoritativeSteps = new();

        public AuthoritativeStep(TickId tickId)
        {
            appliedAtTickId = tickId;
        }

        public int OctetCount
        {
            get
            {
                var totalCount = 0;
                foreach (var authoritativeStep in authoritativeSteps.Values)
                {
                    totalCount += authoritativeStep.payload.Length;
                }

                return totalCount;
            }
        }

        public AuthoritativeStepForOneParticipant GetAuthoritativeStep(ParticipantId participantId)
        {
            return authoritativeSteps[participantId];
        }

        public static string AuthoritativeStepsToString(Dictionary<ParticipantId, AuthoritativeStepForOneParticipant> steps)
        {
            if (steps.Count == 0)
            {
                return "  completely empty";
            }

            var s = "";

            foreach (var (participantId, authoritativeStep) in steps)
            {
                s += $"\n {participantId}: {authoritativeStep}";
            }

            return s;
        }

        public override string ToString()
        {
            return $"{appliedAtTickId}: {AuthoritativeStepsToString(authoritativeSteps)}";
        }
    }

}
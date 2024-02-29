using Nimble.Authoritative.Steps;
using Piot.Nimble.Steps;

namespace Nimble.Authoritative.Steps
{
	public class ParticipantConnection
	{
		public PredictedStepsQueue incomingSteps = new();
		public ParticipantId participantId;

		public ParticipantConnection(ParticipantId participantId)
		{
			this.participantId = participantId;
		}
	}
}
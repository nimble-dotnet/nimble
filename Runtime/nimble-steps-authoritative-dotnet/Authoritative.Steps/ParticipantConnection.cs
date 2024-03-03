using Nimble.Authoritative.Steps;
using Piot.Nimble.Steps;

namespace Nimble.Authoritative.Steps
{
	public class ParticipantConnection
	{
		public PredictedStepsQueue incomingSteps = new();
		public ParticipantId participantId;
		private LocalPlayerIndex localPlayerIndex;

		public ParticipantConnection(ParticipantId participantId, LocalPlayerIndex localPlayerIndex)
		{
			this.participantId = participantId;
			this.localPlayerIndex = localPlayerIndex;
		}
	}
}
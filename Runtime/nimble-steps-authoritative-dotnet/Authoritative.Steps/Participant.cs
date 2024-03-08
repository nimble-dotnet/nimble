using Piot.Nimble.Steps;

namespace Nimble.Authoritative.Steps
{
	public class Participant
	{
		public PredictedStepsQueue incomingSteps = new();
		public ParticipantId participantId;
		private LocalPlayerIndex localPlayerIndex;

		public Participant(ParticipantId participantId, LocalPlayerIndex localPlayerIndex)
		{
			this.participantId = participantId;
			this.localPlayerIndex = localPlayerIndex;
		}

		public override string ToString()
		{
			return $"[Participant {participantId} (localPlayerIndex:{localPlayerIndex} waitingSteps: {incomingSteps.Count})]";
		}
	}
}
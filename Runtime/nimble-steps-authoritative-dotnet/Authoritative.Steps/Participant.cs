using Piot.Nimble.Steps;

namespace Nimble.Authoritative.Steps
{
	public class Participant
	{
		public PredictedStepsQueue incomingSteps = new(new(0));
		public ParticipantId participantId;
		private LocalPlayerIndex localPlayerIndex;
		private uint penaltyTickCount;

		public Participant(ParticipantId participantId, LocalPlayerIndex localPlayerIndex)
		{
			this.participantId = participantId;
			this.localPlayerIndex = localPlayerIndex;
		}

		public override string ToString()
		{
			return $"[Participant {participantId} (localPlayerIndex:{localPlayerIndex} waitingSteps: {incomingSteps.Count})]";
		}

		public void AddPenalty()
		{
			penaltyTickCount++;
		}

		public uint Penalty => penaltyTickCount;

		public void ClearPenalty()
		{
			penaltyTickCount = 0;
		}
	}
}
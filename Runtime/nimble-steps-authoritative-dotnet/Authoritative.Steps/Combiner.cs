using Piot.Clog;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	public struct ParticipantId
	{
		public byte id;


		public ParticipantId(byte id)
		{
			this.id = id;
		}

		public override string ToString()
		{
			return $"[participantId {id}]";
		}
	}


	public enum SerializeProviderConnectState
	{
		Normal,
		StepNotProvidedInTime,
		StepWaitingForReconnect
	}

	public class Combiner
	{
		public static AuthoritativeStep ComposeOneAuthoritativeSteps(
			Participants connections, TickId tickIdToCompose, ILog log)
		{
			var combined = new AuthoritativeStep(tickIdToCompose);

			log.DebugLowLevel("composing step {TickID}", tickIdToCompose);
			foreach (var (participantId, participantConnection) in connections.participants)
			{
				var incomingPredictedSteps = participantConnection.incomingSteps;
				SerializeProviderConnectState connectState = SerializeProviderConnectState.Normal;

				AuthoritativeStepForOneParticipant foundStepForOneParticipant = default;

				if(!incomingPredictedSteps.HasStepForTickId(tickIdToCompose))
				{
					log.DebugLowLevel("participant {ParticipantId} did not have a step for {tickIdToCompose} {Queue}", participantId,
						tickIdToCompose, incomingPredictedSteps);
					connectState = SerializeProviderConnectState.StepNotProvidedInTime;
					foundStepForOneParticipant = new AuthoritativeStepForOneParticipant(tickIdToCompose, connectState);
				}
				else
				{
					log.DebugLowLevel("participant {ParticipantId} did actually have a step for {tickIdToCompose}", participantId,
						tickIdToCompose);
					var predictedStep = incomingPredictedSteps.GetInputFromTickId(tickIdToCompose);
					foundStepForOneParticipant = new AuthoritativeStepForOneParticipant(tickIdToCompose, predictedStep.payload.Span);
				}

				//log.DebugLowLevel("discarding input up to (excluding) {TickID}", tickIdToCompose.Next);
				incomingPredictedSteps.DiscardUpToAndExcluding(tickIdToCompose.Next);

				combined.authoritativeSteps.Add(new(participantId), foundStepForOneParticipant);
			}

			return combined;
		}
	}
}
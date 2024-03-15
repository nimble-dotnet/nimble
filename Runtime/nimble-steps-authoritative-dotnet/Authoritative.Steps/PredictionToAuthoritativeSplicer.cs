using Piot.Clog;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	public enum SerializeProviderConnectState
	{
		Normal,
		StepNotProvidedInTime,
		StepWaitingForReconnect
	}

	public class PredictionToAuthoritativeSplicer
	{
		public static AuthoritativeStep SpliceOneAuthoritativeSteps(
			Participants connections, TickId tickIdToCompose, ILog log)
		{
			var authoritativeStep = new AuthoritativeStep(tickIdToCompose);

			log.DebugLowLevel("composing step {TickID}", tickIdToCompose);
			foreach (var (participantId, participantConnection) in connections.participants)
			{
				var incomingPredictedSteps = participantConnection.incomingSteps;

				AuthoritativeStepForOneParticipant foundStepForOneParticipant = default;

				if(!incomingPredictedSteps.HasStepForTickId(tickIdToCompose))
				{
					log.DebugLowLevel("participant {ParticipantId} did not have a step for {tickIdToCompose} {Queue}", participantId,
						tickIdToCompose, incomingPredictedSteps);
					foundStepForOneParticipant = new AuthoritativeStepForOneParticipant(tickIdToCompose, SerializeProviderConnectState.StepNotProvidedInTime);
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

				authoritativeStep.authoritativeSteps.Add(new(participantId), foundStepForOneParticipant);
			}

			return authoritativeStep;
		}
	}
}
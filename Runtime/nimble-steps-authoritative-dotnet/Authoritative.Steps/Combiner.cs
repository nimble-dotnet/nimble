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
		public static CombinedAuthoritativeStep ComposeOneAuthoritativeSteps(
			Participants connections, TickId tickIdToCompose, ILog log)
		{
			var combined = new CombinedAuthoritativeStep(tickIdToCompose);

			foreach (var (participantId, participantConnection) in connections.participants)
			{
				var incomingPredictedSteps = participantConnection.incomingSteps;
				SerializeProviderConnectState connectState = SerializeProviderConnectState.Normal;

				AuthoritativeStep foundStep = default;

				if(!incomingPredictedSteps.HasStepForTickId(tickIdToCompose))
				{
//					log.Warn("participant {ParticipantId} did not have a step for {tickIdToCompose} {Queue}", participantId,
//						tickIdToCompose, incomingPredictedSteps);
					connectState = SerializeProviderConnectState.StepNotProvidedInTime;
					foundStep = new AuthoritativeStep(tickIdToCompose, connectState);
				}
				else
				{
//					log.Warn("participant {ParticipantId} did actually have a step for {tickIdToCompose}", participantId,
//						tickIdToCompose);
					var predictedStep = incomingPredictedSteps.GetInputFromTickId(tickIdToCompose);
					foundStep = new AuthoritativeStep(tickIdToCompose, predictedStep.payload.Span);
				}

//				log.Warn("discarding input up to (excluding) {TickID}", tickIdToCompose.Next);
				incomingPredictedSteps.DiscardUpToAndExcluding(tickIdToCompose.Next);

				combined.authoritativeSteps.Add(new(participantId), foundStep);
			}

			return combined;
		}
	}
}
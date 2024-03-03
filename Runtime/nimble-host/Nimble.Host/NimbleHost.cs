using System;
using System.Collections;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Datagram;
using Piot.Flood;
using Piot.Nimble.Steps;
using Piot.Nimble.Steps.Serialization;
using Piot.Tick;

namespace Piot.Nimble.Host
{
	public class NimbleHost
	{
		public CombinedAuthoritativeStepProducer authoritativeStepProducer;
		public ParticipantConnections participantConnections = new();
		public HostConnections hostConnections = new();

		private HostDatagram fakeDatagram;
		private ILog log;

		public NimbleHost(TickId startId, ILog log)
		{
			authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participantConnections, log);
			this.log = log;
		}

		public void ReceiveDatagram(in HostDatagram datagram)
		{
			var hostConnection = hostConnections.GetOrCreateConnection(datagram.connection);

			var reader = new OctetReader(datagram.payload.Span);

			var predictedStepsForAllLocalPlayers = PredictedStepsDeserialize.Deserialize(reader, log);
			foreach (var predictedStepsForPlayer in predictedStepsForAllLocalPlayers.stepsForEachPlayerInSequence)
			{
				var existingLocalPlayer =
					hostConnection.connectionToParticipants.TryGetParticipantConnectionFromLocalPlayer(
						predictedStepsForPlayer
							.localPlayerIndex, out var participantConnection);
				if(!existingLocalPlayer)
				{
					log.Debug("detected a new local player {{LocalPlayer}} creating a new participant connection",
						predictedStepsForPlayer.localPlayerIndex);
					participantConnection = participantConnections.CreateParticipantConnection(datagram.connection,
						predictedStepsForPlayer.localPlayerIndex);
					hostConnection.connectionToParticipants.Add(predictedStepsForPlayer.localPlayerIndex,
						participantConnection);
				}

				foreach (var predictedStep in predictedStepsForPlayer.steps)
				{
					log.Debug("adding incoming predicted step {{PredictedStepTick}}", predictedStep.appliedAtTickId);
					participantConnection.incomingSteps.AddPredictedStep(predictedStep);
				}
			}
		}

		public void Tick(TickId simulationTickId)
		{
			authoritativeStepProducer.ComposeOneStep();
		}

		public IEnumerable<HostDatagram> DatagramsToSend => new List<HostDatagram> { fakeDatagram };
	}
}
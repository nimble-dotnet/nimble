using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Datagram;
using Piot.Flood;
using Piot.Nimble.Steps.Serialization;
using Piot.Tick;
using UnityEngine;

namespace Piot.Nimble.Host
{
	public class NimbleHost
	{
		public CombinedAuthoritativeStepProducer authoritativeStepProducer;
		public ParticipantConnections participantConnections = new();
		public HostConnections hostConnections = new();
		private List<HostDatagram> outDatagrams = new();
		private OctetWriter outWriter = new(1024);

		private ILog log;

		public NimbleHost(TickId startId, ILog log)
		{
			authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participantConnections, log);
			this.log = log;
		}

		public void ReceiveDatagram(in HostDatagram datagram)
		{
			log.Debug($"receive datagram {{OctetCount}} from {{Connection}}", datagram.payload.Length,
				datagram.connection);
			var hostConnection = hostConnections.GetOrCreateConnection(datagram.connection);

			var reader = new OctetReader(datagram.payload.Span);

			var predictedStepsForAllLocalPlayers = PredictedStepsDeserialize.Deserialize(reader, log);

			log.Debug("received datagram {{PredictedStepsFromClient}}", predictedStepsForAllLocalPlayers);

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
					if(predictedStep.appliedAtTickId < participantConnection.incomingSteps.WaitingForTickId)
					{
						continue;
					}

//					log.Debug("adding incoming predicted step {{LocalPlayerIndex}} {{PredictedStepTick}}",
//						predictedStepsForPlayer.localPlayerIndex, predictedStep.appliedAtTickId);
					participantConnection.incomingSteps.AddPredictedStep(predictedStep);
				}
			}
		}

		public void Tick(TickId simulationTickId)
		{
			authoritativeStepProducer.Tick();

			outDatagrams.Clear();

			var authoritativeStepsQueue = authoritativeStepProducer.AuthoritativeStepsQueue;

			const uint MaximumAuthoritativeSteps = 32;
			// HACK: For now just send the last authoritative steps
			if(authoritativeStepsQueue.Count > MaximumAuthoritativeSteps)
			{
				var diff = authoritativeStepsQueue.Count - MaximumAuthoritativeSteps;
				var oldestTickId = authoritativeStepsQueue.Peek().appliedAtTickId;
				log.Debug("Too many authoritative steps composed, discarding from {{TickID}} {{Count}}", oldestTickId,
					diff);
				authoritativeStepsQueue.DiscardUpToAndExcluding(
					new((uint)(oldestTickId.tickId + diff)));
			}

			var allAuthoritativeSteps = authoritativeStepsQueue.Collection;
			if(allAuthoritativeSteps.Length == 0)
			{
				return;
			}

			var range = new TickIdRange(allAuthoritativeSteps[0].appliedAtTickId,
				allAuthoritativeSteps[^1].appliedAtTickId);

			var combinedSteps = new CombinedAuthoritativeSteps(allAuthoritativeSteps);

			foreach (var (connectionId, hostConnection) in hostConnections.connections)
			{
				// TODO: Find out range for host connection
				var hostRanges = new List<TickIdRange> { range };

				log.Debug("decision is to send combined authoritative step range {{Range}}", range);
				var hostConnectionRange = new TickIdRanges { ranges = hostRanges };

				outWriter.Reset();

				CombinedRangesWriter.Write(combinedSteps, hostConnectionRange, outWriter, log);

				log.Debug("combined authoritative step range {{OctetSize}}", outWriter.Position);

				var outDatagram = new HostDatagram
				{
					connection = connectionId,
					payload = outWriter.Octets.ToArray()
				};

				outDatagrams.Add(outDatagram);
			}
		}

		public IEnumerable<HostDatagram> DatagramsToSend => outDatagrams;
	}
}
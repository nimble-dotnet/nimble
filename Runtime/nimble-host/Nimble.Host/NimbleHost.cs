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
		public Participants participants = new();
		public HostConnections hostConnections = new();
		private List<HostDatagram> outDatagrams = new();
		private OctetWriter outWriter = new(1024);

		private ILog log;

		public NimbleHost(TickId startId, ILog log)
		{
			authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participants, log);
			this.log = log;
		}

		public void ReceiveDatagram(in HostDatagram datagram)
		{
			log.DebugLowLevel("receive datagram {OctetCount} from {Connection}", datagram.payload.Length,
				datagram.connection);
			var hostConnection = hostConnections.GetOrCreateConnection(datagram.connection);

			var reader = new OctetReader(datagram.payload.Span);

			var predictedStepsForAllLocalPlayers = PredictedStepsDeserialize.Deserialize(reader, log);

			log.DebugLowLevel("received predicted steps {PredictedStepsFromClient}", predictedStepsForAllLocalPlayers);

			foreach (var predictedStepsForPlayer in predictedStepsForAllLocalPlayers.stepsForEachPlayerInSequence)
			{
				var hasExistingParticipant =
					hostConnection.connectionToParticipants.TryGetParticipantConnectionFromLocalPlayer(
						predictedStepsForPlayer
							.localPlayerIndex, out var participant);
				if(!hasExistingParticipant)
				{
					participant = participants.CreateParticipant(datagram.connection,
						predictedStepsForPlayer.localPlayerIndex);

					log.Info("detect new local player index, creating a new {Participant} for {Connection} and {LocalIndex}",
						participant, hostConnection, predictedStepsForPlayer.localPlayerIndex);
					hostConnection.connectionToParticipants.Add(predictedStepsForPlayer.localPlayerIndex,
						participant);
				}

				foreach (var predictedStep in predictedStepsForPlayer.steps)
				{
					if(predictedStep.appliedAtTickId < participant.incomingSteps.WaitingForTickId)
					{
						continue;
					}

					log.DebugLowLevel("adding incoming predicted step {Participant} {PredictedStepTick}",
						participant, predictedStep.appliedAtTickId);
					participant.incomingSteps.AddPredictedStep(predictedStep);
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
				log.Debug("Too many authoritative steps composed, discarding from {TickID} {Count}", oldestTickId,
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
			log.DebugLowLevel("total authoritative steps in NimbleHost {Range}", range);

			var combinedSteps = new CombinedAuthoritativeSteps(allAuthoritativeSteps);

			foreach (var (connectionId, hostConnection) in hostConnections.connections)
			{
				// TODO: Find out range for host connection
				var hostRanges = new List<TickIdRange> { range };

				log.Debug("decision is to send combined authoritative step range {Range}", range);
				var hostConnectionRange = new TickIdRanges { ranges = hostRanges };

				outWriter.Reset();

				CombinedRangesWriter.Write(combinedSteps, hostConnectionRange, outWriter, log);

				log.Debug("combined authoritative step range {OctetSize}", outWriter.Position);

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
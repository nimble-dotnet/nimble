using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
using Piot.Nimble.Steps.Serialization;
using Piot.OrderedDatagrams;
using Piot.Tick;

namespace Piot.Nimble.Host
{
	public class NimbleHost
	{
		public CombinedAuthoritativeStepProducer authoritativeStepProducer;
		public Participants participants;
		public HostConnections hostConnections = new();
		private const int MaximumOutDatagramCount = 16 * 3;
		private CircularBuffer<HostDatagram> outDatagrams = new(MaximumOutDatagramCount);
		private OctetWriter outWriter = new(1024);
		private OrderedDatagramsSequenceId datagramSequenceIdOut;

		private ILog log;

		public NimbleHost(TickId startId, ILog log)
		{
			participants = new Participants(log);
			authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participants, log);
			this.log = log;
		}

		public void ReceiveDatagram(in HostDatagram datagram)
		{
//            log.Warn("receive datagram {OctetCount} from {Connection}", datagram.payload.Length,
			//              datagram.connection);
			var hostConnection = hostConnections.GetOrCreateConnection(datagram.connection);

			var reader = new OctetReader(datagram.payload.Span);

			OrderedDatagramsSequenceIdReader.Read(reader);

			hostConnection.lastReceivedMonotonicLowerBits = MonotonicTimeLowerBitsReader.Read(reader);

			StatusReader.Read(reader, out var expectingTickId, out var droppedTicksAfterThat);
			hostConnection.expectingAuthoritativeTickId = expectingTickId;
			hostConnection.dropppedAuthoritativeAfterExpecting = droppedTicksAfterThat;


			var predictedStepsForAllLocalPlayers = PredictedStepsDeserialize.Deserialize(reader, log);

			//      log.Warn("received predicted steps {PredictedStepsFromClient}", predictedStepsForAllLocalPlayers);

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

					log.Info(
						"detect new local player index, creating a new {Participant} for {Connection} and {LocalIndex}",
						participant, hostConnection, predictedStepsForPlayer.localPlayerIndex);
					hostConnection.connectionToParticipants.Add(predictedStepsForPlayer.localPlayerIndex,
						participant);
				}

				foreach (var predictedStep in predictedStepsForPlayer.steps)
				{
					if(predictedStep.appliedAtTickId < participant.incomingSteps.WaitingForTickId)
					{
						//log.Warn("skipping {PredictedTickId} since waiting for {WaitingForTickId}",
						//  predictedStep.appliedAtTickId, participant.incomingSteps.WaitingForTickId);
						continue;
					}

//                    log.Warn("adding incoming predicted step {Participant} {PredictedStepTick}",
					//                      participant, predictedStep.appliedAtTickId);
					participant.incomingSteps.AddPredictedStep(predictedStep);
				}
			}
		}

		public void Tick(TickId simulationTickId)
		{
			var authoritativeStepsQueue = authoritativeStepProducer.AuthoritativeStepsQueue;

			authoritativeStepProducer.Tick();

			if(authoritativeStepsQueue.IsEmpty)
			{
				return;
			}

			var authoritativeRangeInBuffer = authoritativeStepsQueue.Range;
//            log.Warn("total authoritative steps in NimbleHost {Range}", range);

			outDatagrams.Clear();
			foreach (var (connectionId, hostConnection) in hostConnections.connections)
			{
				if(hostConnection.expectingAuthoritativeTickId < authoritativeRangeInBuffer.startTickId)
				{
					log.Notice("{Connection} is way behind, it wants {TickID}", hostConnection,
						hostConnection.expectingAuthoritativeTickId);
				}

				var startId = hostConnection.expectingAuthoritativeTickId;
				var lastAuthoritativeTickId = hostConnection.expectingAuthoritativeTickId + 20;
				if(lastAuthoritativeTickId > authoritativeRangeInBuffer.Last)
				{
					lastAuthoritativeTickId = authoritativeRangeInBuffer.Last;
				}

				if(startId > lastAuthoritativeTickId)
				{
					startId = lastAuthoritativeTickId;
				}

				var specialRange =
					new TickIdRange(startId, lastAuthoritativeTickId);

				var hostRanges = new List<TickIdRange> { specialRange };

				//              log.Warn("decision is to send combined authoritative step range {Range}", range);
				var hostConnectionRange = new TickIdRanges { ranges = hostRanges };

				outWriter.Reset();

				OrderedDatagramsSequenceIdWriter.Write(outWriter, datagramSequenceIdOut);
				datagramSequenceIdOut.Next();
				MonotonicTimeLowerBitsWriter.Write(hostConnection.lastReceivedMonotonicLowerBits, outWriter);
				CombinedRangesWriter.Write(authoritativeStepsQueue, hostConnectionRange, outWriter, log);

				//            log.Warn("combined authoritative step range {OctetSize}", outWriter.Position);

				ref var outDatagram = ref outDatagrams.EnqueueRef();
				outDatagram.connection = connectionId;
				outDatagram.payload = outWriter.Octets.ToArray();
			}
		}

		public IEnumerable<HostDatagram> DatagramsToSend => outDatagrams;
	}
}
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
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
        private ILog log;

        public TickId WaitingToComposeTickId => authoritativeStepProducer.AuthoritativeStepsQueue.WaitingForTickId;


        public NimbleHost(TickId startId, ILog log)
        {
            //
            participants = new Participants(log);
            authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participants, log);
            this.log = log;
        }

        public void ReceiveDatagram(in HostDatagram datagram)
        {
            log.DebugLowLevel("receive datagram {OctetCount} from {Connection}", datagram.payload.Length,
                datagram.connection);
            var hostConnection = hostConnections.GetOrCreateConnection(datagram.connection);

            var reader = new OctetReader(datagram.payload.Span);

            OrderedDatagramsSequenceIdReader.Read(reader);

            hostConnection.lastReceivedMonotonicLowerBits = MonotonicTimeLowerBitsReader.Read(reader);

            StatusReader.Read(reader, out var expectingTickId, out var droppedTicksAfterThat);
            hostConnection.expectingAuthoritativeTickId = expectingTickId;
            hostConnection.dropppedAuthoritativeAfterExpecting = droppedTicksAfterThat;

            var highestTickId = PredictedStepsDeserialize.Deserialize(reader,
                hostConnection.connectionId,
                hostConnection.connectionToParticipants, participants, log);
            if (highestTickId > hostConnection.lastReceivedPredictedTickId)
            {
                hostConnection.lastReceivedPredictedTickId = highestTickId;
            }
        }

        public void Tick(TickId simulationTickId)
        {
            var authoritativeStepsQueue = authoritativeStepProducer.AuthoritativeStepsQueue;

            authoritativeStepProducer.Tick();

            if (authoritativeStepsQueue.IsEmpty)
            {
                return;
            }

            var authoritativeRangeInBuffer = authoritativeStepsQueue.Range;
//            log.Warn("total authoritative steps in NimbleHost {Range}", range);

            outDatagrams.Clear();
            foreach (var (connectionId, hostConnection) in hostConnections.connections)
            {
                if (hostConnection.expectingAuthoritativeTickId < authoritativeRangeInBuffer.startTickId)
                {
                    log.Notice(
                        "{Connection} is way behind, it is waiting for {TickID} and authoritative buffer only has {Range}",
                        hostConnection,
                        hostConnection.expectingAuthoritativeTickId, authoritativeRangeInBuffer);
                }

                var startId = hostConnection.expectingAuthoritativeTickId;
                var lastAuthoritativeTickId = startId + 40;
                var requestedRange = new TickIdRange(startId, lastAuthoritativeTickId);

                var rangeToSend = authoritativeRangeInBuffer.Satisfy(requestedRange);

                var hostRanges = new List<TickIdRange> { rangeToSend };

                //              log.Warn("decision is to send combined authoritative step range {Range}", range);
                var hostConnectionRange = new TickIdRanges { ranges = hostRanges };

                outWriter.Reset();

                OrderedDatagramsSequenceIdWriter.Write(outWriter, hostConnection.datagramSequenceIdOut);
                hostConnection.datagramSequenceIdOut.Next();
                MonotonicTimeLowerBitsWriter.Write(hostConnection.lastReceivedMonotonicLowerBits, outWriter);
                WriteParticipantInfo(hostConnection, outWriter);
                WriteBufferInfo(hostConnection, outWriter);
                CombinedRangesWriter.Write(authoritativeStepsQueue, hostConnectionRange, outWriter, log);

                //            log.Warn("combined authoritative step range {OctetSize}", outWriter.Position);

                ref var outDatagram = ref outDatagrams.EnqueueRef();
                outDatagram.connection = connectionId;
                outDatagram.payload = outWriter.Octets.ToArray();
            }
        }

        private void WriteBufferInfo(HostConnection hostConnection, OctetWriter writer)
        {
            var lastReceivedTickId = hostConnection.lastReceivedPredictedTickId;
            var authoritativeStepsQueue = authoritativeStepProducer.AuthoritativeStepsQueue;
            var diff = (int)lastReceivedTickId.tickId - (int)authoritativeStepsQueue.WaitingForTickId.tickId + 1;

            diff = diff switch
            {
                > 127 => 127,
                < -127 => -127,
                _ => diff
            };

            log.DebugLowLevel("writing {BufferInfo} {LastReceived} {WaitingAuthoritativeTickId}", diff,
                lastReceivedTickId, authoritativeStepsQueue.WaitingForTickId);
            writer.WriteInt8((sbyte)diff);
        }

        static void WriteParticipantInfo(HostConnection hostConnection, OctetWriter writer)
        {
            var connectionParticipants = hostConnection.connectionToParticipants.connections;
            writer.WriteUInt8((byte)connectionParticipants.Count);
            foreach (var (localPlayerIndex, participant) in connectionParticipants)
            {
                writer.WriteUInt8(localPlayerIndex.Value);
                writer.WriteUInt8(participant.participantId.id);
            }
        }

        public IEnumerable<HostDatagram> DatagramsToSend => outDatagrams;
    }
}
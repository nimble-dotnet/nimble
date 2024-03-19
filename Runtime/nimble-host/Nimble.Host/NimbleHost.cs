/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
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
    /// <summary>
    /// Implements a Host for the Nimble protocol.
    /// </summary>
    public class NimbleHost
    {
        /// <summary>
        /// The authoritative step producer that combines multiple predicted steps and combines them into one.
        /// </summary>
        public AuthoritativeStepProducer authoritativeStepProducer;
        
        /// <summary>
        /// The participants connected to the host
        /// </summary>
        public Participants participants;

        /// <summary>
        /// The incoming connections that has been established.
        /// </summary>
        public HostConnections hostConnections = new();
        private const int MaximumOutDatagramCount = 16 * 3;
        private CircularBuffer<HostDatagram> outDatagrams = new(MaximumOutDatagramCount);
        private OctetWriter outWriter = new(1024);
        private ILog log;

        public TickId WaitingToComposeTickId => authoritativeStepProducer.AuthoritativeStepsQueue.WaitingForTickId;

        /// <summary>
        /// Initializes a new instance of the <see cref="NimbleHost"/> class.
        /// </summary>
        /// <param name="startId">The starting tick ID. The first TickID that the host and producer is waiting for.</param>
        /// <param name="log">The log.</param>
        public NimbleHost(TickId startId, ILog log)
        {
            //
            participants = new Participants(log);
            authoritativeStepProducer = new AuthoritativeStepProducer(startId, participants, log);
            this.log = log;
        }

        /// <summary>
        /// Receives a datagram. Fills the participants prediction buffers.
        /// </summary>
        /// <param name="datagram">The datagram to receive.</param>
        public void ReceiveDatagram(in HostDatagram datagram)
        {
            log.DebugLowLevel("receive datagram {OctetCount} from {Connection}", datagram.payload.Length,
                datagram.connection);
            var hostConnection = hostConnections.GetOrCreateConnection(datagram.connection);

            var reader = new OctetReader(datagram.payload);

            OrderedDatagramsSequenceIdReader.Read(reader);

            hostConnection.lastReceivedMonotonicLowerBits = MonotonicTimeLowerBitsReader.Read(reader);

            StatusReader.Read(reader, out var expectingTickId, out var droppedTicksAfterThat);
            hostConnection.expectingAuthoritativeTickId = expectingTickId;
            hostConnection.dropppedAuthoritativeAfterExpecting = droppedTicksAfterThat;

            var highestTickId = PredictedStepsReader.Read(reader,
                hostConnection.connectionId,
                hostConnection.connectionToParticipants, participants, log);
            if (highestTickId > hostConnection.lastReceivedPredictedTickId)
            {
                hostConnection.lastReceivedPredictedTickId = highestTickId;
            }
        }

        /// <summary>
        /// Tries to produce as much authoritative ticks as possible and adds outDatagrams with outgoing authoritative steps.
        /// </summary>
        /// <param name="simulationTickId">The tick ID.</param>
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
                if (startId > authoritativeStepsQueue.Last.appliedAtTickId)
                {
                    startId = authoritativeStepsQueue.Last.appliedAtTickId;
                }

                //              log.Warn("decision is to send combined authoritative step range {Range}", range);

                outWriter.Reset();

                OrderedDatagramsSequenceIdWriter.Write(outWriter, hostConnection.datagramSequenceIdOut);
                hostConnection.datagramSequenceIdOut.Next();
                MonotonicTimeLowerBitsWriter.Write(hostConnection.lastReceivedMonotonicLowerBits, outWriter);
                WriteParticipantInfo(hostConnection, outWriter);
                WriteBufferInfo(hostConnection, outWriter);
                AuthoritativeStepsWriter.Write(authoritativeStepsQueue, startId, outWriter, log);

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

        /// <summary>
        /// Gets the datagrams to send over the transport
        /// </summary>
        public IEnumerable<HostDatagram> DatagramsToSend => outDatagrams;
    }
}
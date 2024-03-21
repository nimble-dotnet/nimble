/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.BlobStream;
using Piot.Clog;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.Serialize;
using Piot.OrderedDatagrams;
using Piot.Tick;
using Constants = Piot.Nimble.Serialize.Constants;

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
        private AuthoritativeStepProducer authoritativeStepProducer;

        /// <summary>
        /// The participants connected to the host
        /// </summary>
        private Participants participants;

        /// <summary>
        /// The incoming connections that has been established.
        /// </summary>
        private HostConnections hostConnections = new();

        private const int MaximumPlayers = 16;
        private const int MaximumOutDatagramCount = MaximumPlayers * 4;
        private CircularBuffer<HostDatagram> outDatagrams = new(MaximumOutDatagramCount);
        private OctetWriter outWriter = new(1024);
        private ILog log;

        private BlobStreamSenderChunks serializedSavedState;
        public TickId WaitingToComposeTickId => authoritativeStepProducer.AuthoritativeStepsQueue.WaitingForTickId;

        /// <summary>
        /// Initializes a new instance of the <see cref="NimbleHost"/> class.
        /// </summary>
        /// <param name="startId">The starting tick ID. The first TickID that the host and producer is waiting for.</param>
        /// <param name="log">The log.</param>
        public NimbleHost(TickId startId, ReadOnlySpan<byte> simulationSerializedSaveState, ILog log)
        {
            //
            serializedSavedState = new BlobStreamSenderChunks(simulationSerializedSaveState, Constants.OptimalChunkSize);
            participants = new Participants(log);
            authoritativeStepProducer = new AuthoritativeStepProducer(startId, participants, log);
            this.log = log;
        }

        /// <summary>
        /// Receives a datagram. Fills the participants prediction buffers.
        /// </summary>
        /// <param name="datagram">The datagram to receive.</param>
        public void ReceiveDatagram(in HostDatagram datagram, TimeMs now)
        {
            var foundConnection = hostConnections.TryGetConnection(datagram.connection, out var hostConnection);
            if (!foundConnection)
            {
                hostConnection = new HostConnection(datagram.connection, serializedSavedState,
                    authoritativeStepProducer.AuthoritativeStepsQueue, participants, now,
                    log.SubLog($"Connection({datagram.connection})"));
                hostConnections.AddConnection(hostConnection);
            }

            var reader = new OctetReader(datagram.payload);

            hostConnection.HandleDatagram(reader);
        }


        /// <summary>
        /// Tries to produce as much authoritative ticks as possible and adds outDatagrams with outgoing authoritative steps.
        /// </summary>
        /// <param name="simulationTickId">The tick ID.</param>
        public IEnumerable<HostDatagram> Tick(TickId tickId, TimeMs now)
        {
            log.DebugLowLevel("tick {TickID} {Now}", tickId, now);

            authoritativeStepProducer.Tick();

            outDatagrams.Clear();
            foreach (var (_, hostConnection) in hostConnections.connections)
            {
                hostConnection.WriteDatagrams(outWriter, outDatagrams, now);
            }

            return outDatagrams;
        }
    }
}
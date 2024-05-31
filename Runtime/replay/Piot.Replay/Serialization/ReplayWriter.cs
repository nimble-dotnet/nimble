/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Raff.Stream;
using Piot.SerializableVersion.Serialization;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Piot.Replay.Serialization
{
    /// <summary>
    /// Writes simulation state and authoritative steps.
    /// </summary>
    public sealed class ReplayWriter
    {
        readonly OctetWriter cachedOctetWriter;
        readonly uint framesBetweenCompleteState;
        readonly RaffWriter raffWriter;

        uint packCountSinceCompleteState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayWriter"/> class.
        /// </summary>
        /// <param name="capturedAtTimeMs">The time when the replay capture was started.</param>
        /// <param name="tickId">The tick ID for the complete state.</param>
        /// <param name="completeState">The complete state of the application.</param>
        /// <param name="applicationVersion">The version of the application executable, at least the simulation code.</param>
        /// <param name="serializationOptions">the application serialization version used in the replay.</param>
        /// <param name="writer">The octet writer to use for writing the replay file.</param>
        /// <param name="replayConfig">configuration for how the replay writer should be setup and how often complete states should be written</param>
        public ReplayWriter(TimeMs capturedAtTimeMs, TickId tickId, ReadOnlySpan<byte> completeState,
            ApplicationVersion applicationVersion, ApplicationSerializationOptions serializationOptions,
            IOctetWriter writer, ReplayWriterConfig replayConfig)
        {
            cachedOctetWriter = new(replayConfig.maximumStepOrStateOctetSize);
            framesBetweenCompleteState = replayConfig.framesUntilCompleteState;
            raffWriter = new(writer);
            WriteVersionChunk(applicationVersion, serializationOptions);

            packCountSinceCompleteState = replayConfig.framesUntilCompleteState;
            WriteCompleteSimulationState(capturedAtTimeMs, tickId, completeState);
        }

        /// <summary>
        /// Indicates when a <see cref="WriteAuthoritativeStep" /> should be called.
        /// </summary>
        public bool NeedsCompleteState => framesBetweenCompleteState != 0 &&
                                          packCountSinceCompleteState >= framesBetweenCompleteState;

        bool AllowedToAddCompleteState => framesBetweenCompleteState == 0 || NeedsCompleteState;

        void WriteVersionChunk(ApplicationVersion applicationVersion,
            ApplicationSerializationOptions serializationOptions)
        {
            cachedOctetWriter.Reset();
            VersionWriter.Write(cachedOctetWriter, Constants.ReplayFileVersion);
            OctetMarker.WriteMarker(cachedOctetWriter, 0xbf);
            FixedOctets32Writer.Write(cachedOctetWriter, applicationVersion.a);
            OctetMarker.WriteMarker(cachedOctetWriter, 0xbe);
            FixedOctets32Writer.Write(cachedOctetWriter, serializationOptions.a);
            raffWriter.WriteChunk(Constants.ReplayIcon, Constants.ReplayName, cachedOctetWriter.Octets);
        }

        static void WriteCompleteStateHeader(IOctetWriter writer, TimeMs timeNowMs, TickId tickId)
        {
            writer.WriteUInt8(0x02);
            writer.WriteUInt64((ulong)timeNowMs.ms);
            TickIdWriter.Write(writer, tickId);
        }

        /// <summary>
        /// Writes a complete simulation state to the replay file.
        /// </summary>
        /// <param name="capturedAtTimeMs">The monotonic time when the simulation state was captured.</param>
        /// <param name="tickId">The tick ID when the simulation state were snapshotted.</param>
        /// <param name="payload">The serialized complete simulation state.</param>
        public void WriteCompleteSimulationState(TimeMs capturedAtTimeMs, TickId tickId, ReadOnlySpan<byte> payload)
        {
            if (!AllowedToAddCompleteState)
            {
                throw new("Not allowed to insert complete state now");
            }

            packCountSinceCompleteState = 0;

            cachedOctetWriter.Reset();
            WriteCompleteStateHeader(cachedOctetWriter, capturedAtTimeMs, tickId);
            //totalWriter.WriteUInt32((ushort)completeState.Payload.Length);
            cachedOctetWriter.WriteOctets(payload);

            raffWriter.WriteChunk(Constants.CompleteStateIcon, Constants.CompleteStateName, cachedOctetWriter.Octets);
        }

        static void WriteAuthoritativeStepHeader(OctetWriter writer, TimeMs timeNowMs, TickId tickId)
        {
            writer.WriteUInt8(0x01);
            var lowerBits = MonotonicTimeLowerBits.MonotonicTimeLowerBits.FromTime(timeNowMs);
            MonotonicTimeLowerBitsWriter.Write(lowerBits, writer);
            TickIdWriter.Write(writer, tickId);
        }

        /// <summary>
        /// Writes an authoritative step to the replay file.
        /// </summary>
        /// <param name="timeProcessedMs">The monotonic time when the step was written</param>
        /// <param name="tickId">The tick ID for which the input should be applied before making an update tick in the simulation.</param>
        /// <param name="payload">The serialized authoritative step.</param>
        public void WriteAuthoritativeStep(TimeMs timeProcessedMs, TickId tickId, ReadOnlySpan<byte> payload)
        {
            if (NeedsCompleteState)
            {
                // throw new($"needs complete state now, been {packCountSinceCompleteState} since last one");
            }

            cachedOctetWriter.Reset();
            WriteAuthoritativeStepHeader(cachedOctetWriter, timeProcessedMs, tickId);
            cachedOctetWriter.WriteOctets(payload);
            raffWriter.WriteChunk(Constants.AuthoritativeStepIcon, Constants.AuthoritativeStepName,
                cachedOctetWriter.Octets);
            packCountSinceCompleteState++;
        }

        /// <summary>
        /// Closes the replay writer.
        /// </summary>
        public void Close()
        {
            raffWriter.Close();
        }
    }
}
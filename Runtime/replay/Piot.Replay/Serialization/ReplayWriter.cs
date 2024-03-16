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
    public sealed class ReplayWriter
    {
        readonly OctetWriter cachedOctetWriter = new(16 * 1024);
        readonly uint framesBetweenCompleteState;
        readonly RaffWriter raffWriter;

        uint packCountSinceCompleteState;

        public ReplayWriter(TimeMs capturedAtTimeMs, TickId tickId, ReadOnlySpan<byte> completeState,
            ApplicationVersion applicationVersion,
            IOctetWriter writer,
            uint framesUntilCompleteState = 60)
        {
            framesBetweenCompleteState = framesUntilCompleteState;
            raffWriter = new(writer);
            WriteVersionChunk(applicationVersion);
            packCountSinceCompleteState = 60;
            WriteCompleteState(capturedAtTimeMs, tickId, completeState);
        }

        public bool NeedsCompleteState => framesBetweenCompleteState != 0 &&
                                          packCountSinceCompleteState >= framesBetweenCompleteState;

        bool AllowedToAddCompleteState => framesBetweenCompleteState == 0 || NeedsCompleteState;

        void WriteVersionChunk(ApplicationVersion applicationVersion)
        {
            cachedOctetWriter.Reset();
            VersionWriter.Write(cachedOctetWriter, Constants.ReplayFileVersion);
            ApplicationVersionWriter.Write(cachedOctetWriter, applicationVersion);
            raffWriter.WriteChunk(Constants.ReplayIcon, Constants.ReplayName, cachedOctetWriter.Octets);
        }

        static void WriteCompleteStateHeader(IOctetWriter writer, TimeMs timeNowMs, TickId tickId)
        {
            writer.WriteUInt8(0x02);
            writer.WriteUInt64((ulong)timeNowMs.ms);
            TickIdWriter.Write(writer, tickId);
        }

        public void WriteCompleteState(TimeMs capturedAtTimeMs, TickId tickId, ReadOnlySpan<byte> payload)
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

        public void WriteAuthoritativeStep(TimeMs timeProcessedMs, TickId tickId, ReadOnlySpan<byte> payload)
        {
            if (NeedsCompleteState)
            {
               // throw new($"needs complete state now, been {packCountSinceCompleteState} since last one");
            }

            cachedOctetWriter.Reset();
            WriteAuthoritativeStepHeader(cachedOctetWriter, timeProcessedMs, tickId);
            cachedOctetWriter.WriteOctets(payload);
            raffWriter.WriteChunk(Constants.AuthoritativeStepIcon, Constants.AuthoritativeStepName, cachedOctetWriter.Octets);
            packCountSinceCompleteState++;
        }

        public void Close()
        {
            raffWriter.Close();
        }
    }
}
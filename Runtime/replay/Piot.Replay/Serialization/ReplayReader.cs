/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

#nullable enable
using System;
using System.IO;
using Piot.Clog;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Raff.Stream;
using Piot.SerializableVersion.Serialization;
using Piot.Tick;
using Piot.Tick.Serialization;


namespace Piot.Replay.Serialization
{
    public sealed class ReplayReader
    {
        readonly CompleteStateEntry[] completeStateEntries;
        readonly RaffReader raffReader;
        readonly IOctetReaderWithSeekAndSkip readerWithSeek;
        TimeMs lastReadTimeMs;
        TimeMs lastTimeMsFromDeltaState;
        private ILog log;

        public ReplayReader(ApplicationVersion expectedApplicationVersion,
            IOctetReaderWithSeekAndSkip readerWithSeek, ILog log)
        {
            this.log = log;
            this.readerWithSeek = readerWithSeek;
            raffReader = new(readerWithSeek);
            ReadVersionInfo();

            if (expectedApplicationVersion.a != ApplicationVersion.a)
            {
                throw new(
                    $"version mismatch, can not use this replay file {ApplicationVersion} vs expected {expectedApplicationVersion}");
            }


            var shouldScanFile = false;
            if (shouldScanFile)
            {
                var positionBefore = readerWithSeek.Position;
                completeStateEntries =
                    CompleteStateScanner.ScanForAllCompleteStatePositions(raffReader, readerWithSeek);
                readerWithSeek.Seek(positionBefore);
                Range = new(new(completeStateEntries[0].tickId), new(completeStateEntries[^1].tickId));
            }
        }

        public TickIdRange Range { get; }

        public ApplicationVersion ApplicationVersion { get; private set; }

        public TickId FirstCompleteStateTickId => new(completeStateEntries[0].tickId);

        void ReadVersionInfo()
        {
            var versionPack = raffReader.ReadExpectedChunk(Constants.ReplayIcon, Constants.ReplayName);
            var reader = new OctetReader(versionPack);
            var stateSerializationVersion = VersionReader.Read(reader);
            if (!stateSerializationVersion.IsEqualDisregardSuffix(Constants.ReplayFileVersion))
            {
                throw new Exception(
                    $"wrong replay file version {stateSerializationVersion} vs {Constants.ReplayFileVersion}");
            }

            ApplicationVersion = ApplicationVersionReader.Read(reader);
        }


        public CompleteStateEntry FindClosestCompleteStateEntry(TickId tickId)
        {
            var tickIdValue = tickId.tickId;
            if (completeStateEntries.Length == 0)
            {
                throw new("unexpected that no complete states are found");
            }

            var left = 0;
            var right = completeStateEntries.Length - 1;

            var tryCount = 0;
            while (left != right && Math.Abs(left - right) >= 1 && tryCount < 20)
            {
                tryCount++;
                var middle = (left + right) / 2;
                var middleEntry = completeStateEntries[middle];
                if (tickIdValue == middleEntry.tickId)
                {
                    return middleEntry;
                }

                if (tickIdValue < middleEntry.tickId)
                {
                    right = middle;
                }
                else
                {
                    left = middle;
                }
            }

            var closest = completeStateEntries[left];
            if (closest.tickId <= tickIdValue)
            {
                return closest;
            }

            if (left < 1)
            {
                return closest;
            }

            var previous = completeStateEntries[left - 1];
            if (previous.tickId > tickIdValue)
            {
                throw new("strange state in replay");
            }

            return previous;
        }

        public ReadOnlySpan<byte> SeekToClosestCompleteState(TickId closestToTick, out TimeMs capturedAtTime,
            out TickId tickId)
        {
            var findClosestEntry = FindClosestCompleteStateEntry(closestToTick);
            readerWithSeek.Seek(findClosestEntry.streamPosition);

            return ReadCompleteState(out capturedAtTime, out tickId);
        }

        public void SeekToCompleteState(CompleteStateEntry entry)
        {
            readerWithSeek.Seek(entry.streamPosition);
        }

        public bool TryReadNextAuthoritativeStep(out TimeMs capturedAtTime, out TickId tickId,
            out ReadOnlySpan<byte> payload)
        {
            while (true)
            {
                uint octetLength;
                try
                {
                    octetLength = raffReader.ReadChunkHeader(out var icon, out var name);
                    if (icon.Value == 0 && name.Value == 0)
                    {
                        capturedAtTime = default;
                        tickId = default;
                        payload = default;
                        return false;
                    }

                    if (icon.Value == Constants.CompleteStateIcon.Value)
                    {
                        // Skip complete states, we only need the delta state
                        readerWithSeek.Seek(readerWithSeek.Position + octetLength);
                        continue;
                    }
                }
                catch (EndOfStreamException)
                {
                    log.Error("Unexpected end of stream. replay file was probably not closed properly?");
                    capturedAtTime = default;
                    tickId = default;
                    payload = default;
                    return false;
                }

                var beforePosition = readerWithSeek.Position;

                var type = readerWithSeek.ReadUInt8();
                if (type != 01)
                {
                    throw new($"desync {type}");
                }

                var timeLowerBits = MonotonicTimeLowerBitsReader.Read(readerWithSeek);
                lastTimeMsFromDeltaState =
                    LowerBitsToMonotonic.LowerBitsToPastMonotonicMs(lastTimeMsFromDeltaState, timeLowerBits);
                capturedAtTime = lastTimeMsFromDeltaState;
                tickId = TickIdReader.Read(readerWithSeek);

                var afterPosition = readerWithSeek.Position;
                var headerOctetCount = (int)(afterPosition - beforePosition);

                payload = readerWithSeek.ReadOctets((int)octetLength - headerOctetCount);

                return true;
            }
        }

        ReadOnlySpan<byte> ReadCompleteState(out TimeMs capturedAtTime, out TickId tickId)
        {
            var octetLength =
                raffReader.ReadExpectedChunkHeader(Constants.CompleteStateIcon, Constants.CompleteStateName);
            var beforePosition = readerWithSeek.Position;

            var type = readerWithSeek.ReadUInt8();
            if (type != 02)
            {
                throw new("desync");
            }

            capturedAtTime = new TimeMs((long)readerWithSeek.ReadUInt64());
            lastReadTimeMs = capturedAtTime;
            lastTimeMsFromDeltaState = capturedAtTime;
            tickId = TickIdReader.Read(readerWithSeek);

            var afterPosition = readerWithSeek.Position;

            var headerOctetCount = (int)(afterPosition - beforePosition);

            return readerWithSeek.ReadOctets((int)octetLength - headerOctetCount);
        }
    }
}
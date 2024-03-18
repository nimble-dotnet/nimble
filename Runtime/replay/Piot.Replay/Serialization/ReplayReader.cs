/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

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
    /// <summary>
    /// Reads simulation state and authoritative steps from a replay file.
    /// </summary>
    public sealed class ReplayReader
    {
        public enum ScanOptions
        {
            Scan,
            DoNotScan,
            ScanButAllowIncompleteFile,
        }
        readonly SimulationStateEntry[] simulationStateEntries;
        readonly RaffReader raffReader;
        readonly IOctetReaderWithSeekAndSkip readerWithSeek;
        TimeMs lastReadTimeMs;
        TimeMs lastTimeMsFromDeltaState;
        private ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayReader"/> class.
        /// </summary>
        /// <param name="expectedApplicationVersion">The expected application executable version.</param>
        /// <param name="readerWithSeek">The octet reader with seek capability.</param>
        /// <param name="options">The application specific serialization options.</param>
        /// <param name="log">Logger.</param>
        /// <param name="scanOptions">How and if a scan for all simulation state should .</param>
        public ReplayReader(ApplicationVersion expectedApplicationVersion,
            IOctetReaderWithSeekAndSkip readerWithSeek, out ApplicationSerializationOptions options, ILog log, ScanOptions scanOptions = ScanOptions.Scan)
        {
            this.log = log;
            this.readerWithSeek = readerWithSeek;
            raffReader = new(readerWithSeek);
            options = ReadVersionInfo();

            if (expectedApplicationVersion.a != ApplicationVersion.a)
            {
                throw new(
                    $"version mismatch, can not use this replay file {ApplicationVersion} vs expected {expectedApplicationVersion}");
            }

            if (scanOptions == ScanOptions.DoNotScan)
            {
                return;
            }
            
            var positionBefore = readerWithSeek.Position;
            simulationStateEntries =
                SimulationStateScanner.ScanForSimulationStatesInStream(raffReader, readerWithSeek, scanOptions == ScanOptions.ScanButAllowIncompleteFile);
            readerWithSeek.Seek(positionBefore);
            Range = new(new(simulationStateEntries[0].tickId), new(simulationStateEntries[^1].tickId));
        }

        /// <summary>
        /// Gets the tick ID range found in the replay file.
        /// </summary>
        public TickIdRange Range { get; }

        /// <summary>
        /// The application executable found in the replay file.
        /// </summary>
        public ApplicationVersion ApplicationVersion { get; private set; }

        public TickId FirstCompleteStateTickId => new(simulationStateEntries[0].tickId);

        ApplicationSerializationOptions ReadVersionInfo()
        {
            var versionPack = raffReader.ReadExpectedChunk(Constants.ReplayIcon, Constants.ReplayName);
            var reader = new OctetReader(versionPack);
            var stateSerializationVersion = VersionReader.Read(reader);
            if (!stateSerializationVersion.IsEqualDisregardSuffix(Constants.ReplayFileVersion))
            {
                throw new Exception(
                    $"wrong replay file version {stateSerializationVersion} vs {Constants.ReplayFileVersion}");
            }

            OctetMarker.AssertMarker(reader, 0xbf);
            var applicationVersion = new ApplicationVersion();
            applicationVersion.a = FixedOctets32Reader.Read(reader);
            ApplicationVersion = applicationVersion;

            OctetMarker.AssertMarker(reader, 0xbe);
            var applicationSerializationOptions = new ApplicationSerializationOptions();
            applicationSerializationOptions.a = FixedOctets32Reader.Read(reader);

            return applicationSerializationOptions;
        }

        /// <summary>
        /// Finds the closest complete simulation state entry to the given tick ID.
        /// </summary>
        /// <remarks>
        /// If the tick ID should be between two simulation states, it always returns the earlier one (lower value).
        /// </remarks>
        /// <param name="tickId">The tick ID to find the closest complete state to.</param>
        /// <returns>The closest complete state entry.</returns>
        public SimulationStateEntry FindClosestSimulationStateEntry(TickId tickId)
        {
            var tickIdValue = tickId.tickId;
            if (simulationStateEntries.Length == 0)
            {
                throw new InvalidOperationException("Unexpected: No simulation states are found.");
            }

            var left = 0;
            var right = simulationStateEntries.Length - 1;

            while (left < right)
            {
                var middle = (left + right) / 2;
                var middleEntry = simulationStateEntries[middle];
        
                if (tickIdValue == middleEntry.tickId)
                {
                    return middleEntry;
                }

                if (tickIdValue < middleEntry.tickId)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle;
                }
            }

            var closest = simulationStateEntries[left];
            if (closest.tickId > tickIdValue)
            {
                throw new InvalidOperationException("Strange state in replay.");
            }

            return closest;
        }

        /// <summary>
        /// Seeks to the closest complete state entry to the given tick ID and retrieves the complete simulation state.
        /// </summary>
        /// <param name="closestToTick">The tick ID used to find the closest simulation state.</param>
        /// <param name="capturedAtTime">The captured time of the found complete simulation state.</param>
        /// <param name="tickId">The found tick ID of the complete state.</param>
        /// <returns>The payload of the complete state.</returns>
        public ReadOnlySpan<byte> SeekToClosestSimulationState(TickId closestToTick, out TimeMs capturedAtTime,
            out TickId tickId)
        {
            var findClosestEntry = FindClosestSimulationStateEntry(closestToTick);
            readerWithSeek.Seek(findClosestEntry.streamPosition);

            return ReadSimulationState(out capturedAtTime, out tickId);
        }

        /// <summary>
        /// Seeks to a specific complete state entry.
        /// </summary>
        /// <param name="entry">The complete state entry to seek to.</param>
        public void SeekToSimulationState(SimulationStateEntry entry)
        {
            readerWithSeek.Seek(entry.streamPosition);
        }

        /// <summary>
        /// Tries to read the next authoritative step from the replay file.
        /// </summary>
        /// <param name="capturedAtTime">The captured time of the authoritative step.</param>
        /// <param name="tickId">The tick ID of the authoritative step.</param>
        /// <param name="payload">The serialized authoritative step.</param>
        /// <returns>True if an authoritative step was successfully read, otherwise false.</returns>
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

        ReadOnlySpan<byte> ReadSimulationState(out TimeMs capturedAtTime, out TickId tickId)
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
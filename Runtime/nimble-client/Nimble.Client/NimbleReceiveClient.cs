using System;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.OrderedDatagrams;
using Piot.Stats;
using Piot.Tick;

namespace Piot.Nimble.Client
{
    public sealed class NimbleReceiveClient
    {
        public readonly AuthoritativeStepsQueue AuthoritativeStepsQueue;
        private readonly ILog log;
        private readonly NimbleClientReceiveStats receiveStats;
        private OrderedDatagramsInChecker orderedDatagramsInChecker = new();

        public FormattedStat RoundTripTime => receiveStats.RoundTripTime;
        public FormattedStat DatagramCountPerSecond => receiveStats.DatagramCountPerSecond;

        private readonly StatPerSecond datagramBitsPerSecond;

        public IEnumerable<int> RoundTripTimes => receiveStats.roundTripTimes;

        public FormattedStat DatagramBitsPerSecond =>
            new(BitsPerSecondFormatter.Format, datagramBitsPerSecond.Stat);

        private readonly NimbleSendClient sendClient;

        public readonly Dictionary<byte, byte> localIndexToParticipant = new();

        public StatCountThreshold bufferDiff = new(30);
        public StatPerSecond authoritativeTicksPerSecond;

        public FormattedStat AuthoritativeTicksPerSecond =>
            new(StandardFormatterPerSecond.Format, authoritativeTicksPerSecond.Stat);

        private FixedDeltaTimeMs stepTimeMs;

        public int RemotePredictedBufferDiff => bufferDiff.Stat.average;

        /// <summary>
        /// Initializes a new instance of the <see cref="NimbleReceiveClient"/> class.
        /// </summary>
        /// <param name="tickId">The tick ID for the first expected tickID for the authoritative step.</param>
        /// <param name="now">The current time.</param>
        /// <param name="deltaTimeMs">The duration for a single step. Usually 16ms or 32ms.</param>
        /// <param name="sendClient">The send client.</param>
        /// <param name="log">The log.</param>
        public NimbleReceiveClient(TickId tickId, TimeMs now, FixedDeltaTimeMs deltaTimeMs, NimbleSendClient sendClient,
            ILog log)
        {
            this.log = log;
            this.sendClient = sendClient;
            stepTimeMs = deltaTimeMs;
            datagramBitsPerSecond = new StatPerSecond(now, new FixedDeltaTimeMs(500));
            authoritativeTicksPerSecond = new StatPerSecond(now, new FixedDeltaTimeMs(250));
            receiveStats = new NimbleClientReceiveStats(now);
            AuthoritativeStepsQueue = new AuthoritativeStepsQueue(tickId);
        }
        
        public uint TargetPredictStepCount
        {
            get
            {
                const int accountForMissingTheTick = 2;

                if (receiveStats.RoundTripTime.stat.average == 0)
                {
                    return accountForMissingTheTick;
                }

                var tickCountAheadOnHost = bufferDiff.Stat.average;
                var adjustmentForBufferOnHost = 0;
                const int lowerThreshold = 3;
                const int higherThreshold = 6;
                if (tickCountAheadOnHost <= lowerThreshold)
                {
                    adjustmentForBufferOnHost = -(tickCountAheadOnHost - lowerThreshold);
                }
                else if (tickCountAheadOnHost > higherThreshold)
                {
                    adjustmentForBufferOnHost = -(tickCountAheadOnHost - higherThreshold);
                }

                var roundTripInStepCount = receiveStats.RoundTripTime.stat.average / stepTimeMs.ms;
                var target = roundTripInStepCount + adjustmentForBufferOnHost +
                             accountForMissingTheTick;
                log.DebugLowLevel(
                    "target {RoundTripTime} {RoundTripStepCount} {BufferAdjustment} ({TickAheadOnHost}) {AccountForMissingTheTick}",
                    receiveStats.RoundTripTime.stat.average, roundTripInStepCount, adjustmentForBufferOnHost,
                    tickCountAheadOnHost,
                    accountForMissingTheTick);
                return (uint)Math.Clamp(target, 1, 20);
            }
        }

        public void ReceiveDatagram(TimeMs now, ReadOnlySpan<byte> payload)
        {
            //log.DebugLowLevel("Received datagram of {Size}", payload.Length);

            datagramBitsPerSecond.Add(payload.Length * 8);

            var reader = new OctetReader(payload);

            var before = orderedDatagramsInChecker.LastValue;
            var wasOk = orderedDatagramsInChecker.ReadAndCheck(reader, out var diffPackets);
            if (!wasOk)
            {
                log.Notice("unordered packet {Diff}", diffPackets);
                return;
            }

            if (diffPackets > 1)
            {
                log.Notice("dropped {PacketCount} encountered {DatagramID} and last accepted was {LastValue} ",
                    diffPackets - 1, orderedDatagramsInChecker.LastValue, before);
                receiveStats.DroppedDatagrams(diffPackets - 1);
            }

            var pongTimeLowerBits = MonotonicTimeLowerBitsReader.Read(reader);

            ReadParticipantInfo(reader);
            ReadBufferInfo(reader);

            var addedAuthoritativeCount = AuthoritativeStepsReader.Read(AuthoritativeStepsQueue, reader, log);
            log.DebugLowLevel("added {TickCount} authoritative steps", addedAuthoritativeCount);
            authoritativeTicksPerSecond.Add((int)addedAuthoritativeCount);

            if (!AuthoritativeStepsQueue.IsEmpty)
            {
                var last = AuthoritativeStepsQueue.Last.appliedAtTickId;
                sendClient.OnLatestAuthoritativeTickId(last, 0);
            }

            receiveStats.ReceivedPongTime(now, pongTimeLowerBits);

            datagramBitsPerSecond.Update(now);
            authoritativeTicksPerSecond.Update(now);
        }

        private void ReadBufferInfo(OctetReader reader)
        {
            var diff = reader.ReadInt8();
            log.DebugLowLevel("ReadBuffer {Diff}", diff);
            bufferDiff.Add(diff);
        }

        // TODO: Optimize this
        void ReadParticipantInfo(IOctetReader reader)
        {
            localIndexToParticipant.Clear();
            var count = reader.ReadUInt8();
            for (var i = 0; i < count; ++i)
            {
                var localPlayerIndex = reader.ReadUInt8();
                var participantId = reader.ReadUInt8();

                localIndexToParticipant.Add(localPlayerIndex, participantId);
            }
        }

        /// <summary>
        /// Gets the range of the tickIds for the combined authoritative steps in the incoming queue.
        /// </summary>
        /// <returns>The authoritative tickID range.</returns>
        public TickIdRange AuthoritativeRange()
        {
            if (AuthoritativeStepsQueue.IsEmpty)
            {
                return default;
            }

            return AuthoritativeStepsQueue.Range;
        }
    }
}
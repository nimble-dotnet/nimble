using System;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
using Piot.Nimble.Steps;
using Piot.OrderedDatagrams;
using Piot.Stats;
using Piot.Tick;

namespace Piot.Nimble.Client
{
    public sealed class NimbleReceiveClient
    {
        public readonly CombinedAuthoritativeStepsQueue combinedAuthoritativeStepsQueue;
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

        public StatCountThreshold bufferDiff = new(15);
        public StatPerSecond authoritativeTicksPerSecond;
        
        public FormattedStat AuthoritativeTicksPerSecond =>
            new(StandardFormatterPerSecond.Format, authoritativeTicksPerSecond.Stat);

        public int RemotePredictedBufferDiff => bufferDiff.Stat.average;

        public uint TargetPredictStepCount
        {
            get
            {
                if (receiveStats.RoundTripTime.stat.average == 0)
                {
                    return 2;
                }

                var bufferDiffAgainstTarget = bufferDiff.Stat.average - 2;
                
                return (uint)(16 / receiveStats.RoundTripTime.stat.average + 2 - bufferDiffAgainstTarget);
            }
        }

        public NimbleReceiveClient(TickId tickId, TimeMs now, NimbleSendClient sendClient, ILog log)
        {
            this.log = log;
            this.sendClient = sendClient;
            datagramBitsPerSecond = new StatPerSecond(now, new FixedDeltaTimeMs(500));
            authoritativeTicksPerSecond = new StatPerSecond(now, new FixedDeltaTimeMs(250));
            receiveStats = new NimbleClientReceiveStats(now);
            combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
        }

        public void ReceiveDatagram(TimeMs now, ReadOnlySpan<byte> payload)
        {
            log.Debug("Received datagram of {Size}", payload.Length);

            datagramBitsPerSecond.Add(payload.Length * 8);

            var reader = new OctetReader(payload);

            var wasOk = orderedDatagramsInChecker.ReadAndCheck(reader, out var diffPackets);
            if (!wasOk)
            {
                log.Notice("unordered packet {Diff}", diffPackets);
                return;
            }

            if (diffPackets > 1)
            {
                log.Notice("dropped {PacketCount}", diffPackets - 1);
                receiveStats.DroppedPackets(diffPackets - 1);
            }

            var pongTimeLowerBits = MonotonicTimeLowerBitsReader.Read(reader);

            ReadParticipantInfo(reader);
            ReadBufferInfo(reader);

            var addedAuthoritativeCount = CombinedRangesReader.Read(combinedAuthoritativeStepsQueue, reader, log);
            authoritativeTicksPerSecond.Add((int)addedAuthoritativeCount);

            if (!combinedAuthoritativeStepsQueue.IsEmpty)
            {
                var last = combinedAuthoritativeStepsQueue.Last.appliedAtTickId;
                sendClient.OnLatestAuthoritativeTickId(last, 0);
            }

            receiveStats.ReceivedPongTime(now, pongTimeLowerBits);

            datagramBitsPerSecond.Update(now);
            authoritativeTicksPerSecond.Update(now);
        }

        private void ReadBufferInfo(OctetReader reader)
        {
            var diff = reader.ReadInt8();
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

        public TickIdRange AuthoritativeRange()
        {
            if (combinedAuthoritativeStepsQueue.IsEmpty)
            {
                return default;
            }

            return combinedAuthoritativeStepsQueue.Range;
        }
    }
}
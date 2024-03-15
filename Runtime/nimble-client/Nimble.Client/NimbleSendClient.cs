using System;
using System.Collections.Generic;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
using Piot.Nimble.Steps.Serialization;
using Piot.OrderedDatagrams;
using Piot.Stats;
using Piot.Tick;
using Constants = Piot.Datagram.Constants;

namespace Piot.Nimble.Client
{
    /// <summary>
    /// Client that sends data using the Nimble protocol.
    /// </summary>
    public sealed class NimbleSendClient
    {
        public const uint MaxOctetSize = 1024;
        
        /// <summary>
        /// Predicted steps for local players.
        /// </summary>
        public readonly PredictedStepsLocalPlayers predictedSteps;
        private readonly ILog log;
        private readonly OctetWriter octetWriter = new(MaxOctetSize);
        private const int MaximumClientOutDatagramCount = 4;
        private readonly CircularBuffer<ClientDatagram> clientOutDatagrams = new(MaximumClientOutDatagramCount);

 
        private readonly StatPerSecond datagramCountPerSecond;
        private readonly StatPerSecond datagramBitsPerSecond;
        private readonly StatPerSecond predictedStepsSentPerSecond;

        /// <summary>
        /// Formatted statistic for datagram count per second.
        /// </summary>        
        public FormattedStat DatagramCountPerSecond =>
            new(StandardFormatterPerSecond.Format, datagramCountPerSecond.Stat);

        /// <summary>
        /// Formatted statistic for datagram bits per second.
        /// </summary>
        public FormattedStat DatagramBitsPerSecond =>
            new(BitsPerSecondFormatter.Format, datagramBitsPerSecond.Stat);

        private OrderedDatagramsSequenceId datagramSequenceId;
        private TickId expectingAuthoritativeTickId;
        private TickId lastSentPredictedTickId;
        private TickId lastSentPredictedTickIdAddedToStats;

        public FormattedStat PredictedStepsSentPerSecond =>
            new(StandardFormatterPerSecond.Format, predictedStepsSentPerSecond.Stat);

        public NimbleSendClient(TimeMs now, ILog log)
        {
            this.log = log;
            datagramCountPerSecond = new StatPerSecond(now, new(500));
            datagramBitsPerSecond = new StatPerSecond(now, new(500));
            predictedStepsSentPerSecond = new StatPerSecond(now, new(500));
            predictedSteps = new PredictedStepsLocalPlayers(log.SubLog("PredictedStepsLocalPlayers"));
        }

        /// <summary>
        /// Collection of outgoing datagrams.
        /// </summary>
        public IEnumerable<ClientDatagram> OutDatagrams => clientOutDatagrams;


        /// <summary>
        /// Processes a tick. Fills the OutDatagrams.
        /// </summary>
        /// <param name="now">The current time.</param>
        public void Tick(TimeMs now)
        {
            octetWriter.Reset();

            OrderedDatagramsSequenceIdWriter.Write(octetWriter, datagramSequenceId);
            datagramSequenceId.Next();

            MonotonicTimeLowerBitsWriter.Write(
                new((ushort)(now.ms & 0xffff)), octetWriter);

            StatusWriter.Write(octetWriter, expectingAuthoritativeTickId, 0);

            var lastSentTickId = PredictedStepsWriter.Write(octetWriter, predictedSteps, log);
            if (lastSentTickId > lastSentPredictedTickId)
            {
                lastSentPredictedTickId = lastSentTickId;
            }


//			log.Warn($"decision to send predicted steps to send to the host {filteredOutPredictedStepsForLocalPlayers} {{OctetCount}}", octetWriter.Position);

            clientOutDatagrams.Clear();

            if (octetWriter.Position > Constants.MaxDatagramOctetSize)
            {
                throw new Exception($"too many predicted steps to serialize");
            }

            ref var datagram = ref clientOutDatagrams.EnqueueRef();
            datagram.payload = octetWriter.Octets.ToArray();

            var diff = lastSentPredictedTickId.tickId - lastSentPredictedTickIdAddedToStats.tickId;
            if (diff > 0)
            {
                predictedStepsSentPerSecond.Add((int)diff);
            }

            lastSentPredictedTickIdAddedToStats = lastSentPredictedTickId;

            datagramCountPerSecond.Add(1);
            datagramBitsPerSecond.Add(datagram.payload.Length * 8);

            datagramCountPerSecond.Update(now);
            datagramBitsPerSecond.Update(now);
            predictedStepsSentPerSecond.Update(now);
        }

        /// <summary>
        /// Updates the latest authoritative tick ID, which clears the incoming predicted step queues of steps with older TickID.
        /// </summary>
        /// <param name="tickId">The tick ID.</param>
        /// <param name="droppedCount">The dropped count. Not used yet.</param>
        public void OnLatestAuthoritativeTickId(TickId tickId, uint droppedCount)
        {
            predictedSteps.DiscardUpToAndExcluding(tickId.Next);
            expectingAuthoritativeTickId = tickId.Next;
        }
    }
}
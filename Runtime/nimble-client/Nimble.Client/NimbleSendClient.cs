using System;
using System.Collections.Generic;
using Piot.BlobStream;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
using Piot.Nimble.Serialize;
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
        private const int MaximumClientOutDatagramCount = 1;
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
        public BlobStreamReceiveLogic receiveStateLogic;
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

        private void WriteHeader(OctetWriter writer, TimeMs now)
        {
            OrderedDatagramsSequenceIdWriter.Write(octetWriter, datagramSequenceId);
            datagramSequenceId.Next();

            MonotonicTimeLowerBitsWriter.Write(
                new((ushort)(now.ms & 0xffff)), octetWriter);
        }

        /// <summary>
        /// Processes a tick. Fills the OutDatagrams.
        /// </summary>
        /// <param name="now">The current time.</param>
        public IEnumerable<ClientDatagram> Tick(TimeMs now)
        {
            octetWriter.Reset();
            clientOutDatagrams.Clear();

            WriteHeader(octetWriter, now);

            SendAckDownload(octetWriter);
            SendPredictedSteps(octetWriter);


//			log.Warn($"decision to send predicted steps to send to the host {filteredOutPredictedStepsForLocalPlayers} {{OctetCount}}", octetWriter.Position);


            if (octetWriter.Position > Constants.MaxDatagramOctetSize)
            {
                throw new Exception($"too many predicted steps to serialize");
            }

            ref var datagram = ref clientOutDatagrams.EnqueueRef();
            datagram.payload = octetWriter.Octets.ToArray();

            datagramCountPerSecond.Add(1);
            datagramBitsPerSecond.Add(datagram.payload.Length * 8);

            datagramCountPerSecond.Update(now);
            datagramBitsPerSecond.Update(now);
            predictedStepsSentPerSecond.Update(now);

            return clientOutDatagrams;
        }

        private void SendAckDownloadStarted(OctetWriter sendWriter)
        {
            sendWriter.WriteUInt8((byte)ClientToHostRequest.AckSerializedSaveStateStart);
            sendWriter.WriteUInt32(0); // TODO:
        }

        private void SendAckDownload(OctetWriter sendWriter)
        {
            sendWriter.WriteUInt8((byte)ClientToHostRequest.AckSerializedSaveStateBlobStream);
            receiveStateLogic.WriteStream(sendWriter);
        }

        private void SendPredictedSteps(OctetWriter sendWriter)
        {
            sendWriter.WriteUInt8((byte)ClientToHostRequest.RequestAddPredictedStep);
            StatusWriter.Write(sendWriter, expectingAuthoritativeTickId, 0);
            log.DebugLowLevel("Status to host is we expect {AuthoritativeTickID}", expectingAuthoritativeTickId);

            var lastSentTickId = PredictedStepsWriter.Write(sendWriter, predictedSteps, log);
            if (lastSentTickId > lastSentPredictedTickId)
            {
                lastSentPredictedTickId = lastSentTickId;
            }

            var diff = lastSentPredictedTickId.tickId - lastSentPredictedTickIdAddedToStats.tickId;
            if (diff > 0)
            {
                predictedStepsSentPerSecond.Add((int)diff);
            }

            lastSentPredictedTickIdAddedToStats = lastSentPredictedTickId;
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
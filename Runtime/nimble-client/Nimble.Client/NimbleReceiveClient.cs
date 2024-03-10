using System;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
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

		public NimbleReceiveClient(TickId tickId, TimeMs now, NimbleSendClient sendClient, ILog log)
		{
			this.log = log;
			this.sendClient = sendClient;
			datagramBitsPerSecond = new StatPerSecond(now, new FixedDeltaTimeMs(500));
			receiveStats = new NimbleClientReceiveStats(now);
			combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
		}

		public void ReceiveDatagram(TimeMs now, ReadOnlySpan<byte> payload)
		{
			log.Debug("Received datagram of {Size}", payload.Length);

			datagramBitsPerSecond.Add(payload.Length * 8);

			var reader = new OctetReader(payload);

			bool wasOk = orderedDatagramsInChecker.ReadAndCheck(reader, out var diffPackets);
			if(!wasOk)
			{
				log.Notice("unordered packet {Diff}", diffPackets);
				return;
			}

			if(diffPackets > 1)
			{
				log.Notice("dropped {PacketCount}", diffPackets - 1);
				receiveStats.DroppedPackets(diffPackets - 1);
			}

			var pongTimeLowerBits = MonotonicTimeLowerBitsReader.Read(reader);


			CombinedRangesReader.Read(combinedAuthoritativeStepsQueue, reader, log);

			if(!combinedAuthoritativeStepsQueue.IsEmpty)
			{
				var last = combinedAuthoritativeStepsQueue.Last.appliedAtTickId;
				sendClient.OnLatestAuthoritativeTickId(last, 0);
			}

			receiveStats.ReceivedPongTime(now, pongTimeLowerBits);

			datagramBitsPerSecond.Update(now);
		}

		public TickIdRange AuthoritativeRange()
		{
			if(combinedAuthoritativeStepsQueue.IsEmpty)
			{
				return default;
			}

			return combinedAuthoritativeStepsQueue.Range;
		}
	}
}
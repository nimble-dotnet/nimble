using System;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Stats;
using Piot.Tick;

namespace Piot.Nimble.Client
{
	public sealed class NimbleReceiveClient
	{
		public readonly CombinedAuthoritativeStepsQueue combinedAuthoritativeStepsQueue;
		private readonly ILog log;
		private readonly NimbleClientReceiveStats receiveStats;

		public FormattedStat RoundTripTime => receiveStats.RoundTripTime;
		public FormattedStat DatagramCountPerSecond => receiveStats.DatagramCountPerSecond;

		private readonly StatPerSecond datagramBitsPerSecond;

		public FormattedStat DatagramBitsPerSecond =>
			new(BitsPerSecondFormatter.Format, datagramBitsPerSecond.Stat);

		public NimbleReceiveClient(TickId tickId, TimeMs now, ILog log)
		{
			this.log = log;
			datagramBitsPerSecond = new StatPerSecond(now, new FixedDeltaTimeMs(500));
			receiveStats = new NimbleClientReceiveStats(now);
			combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
		}

		public void ReceiveDatagram(TimeMs now, ReadOnlySpan<byte> payload)
		{
			log.Debug("Received datagram of {Size}", payload.Length);

			datagramBitsPerSecond.Add(payload.Length * 8);

			var reader = new OctetReader(payload);

			var pongTimeLowerBits = MonotonicTimeLowerBitsReader.Read(reader);
			CombinedRangesReader.Read(combinedAuthoritativeStepsQueue, reader, log);

			receiveStats.ReceivedPongTime(now, pongTimeLowerBits);

			datagramBitsPerSecond.Update(now);
		}
	}
}
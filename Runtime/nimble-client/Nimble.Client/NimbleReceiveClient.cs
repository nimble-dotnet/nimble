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
		private readonly NimbleClientReceiveStats receiveStats = new();

		public FormattedStat RoundTripTime => receiveStats.RoundTripTime;

		public NimbleReceiveClient(TickId tickId, ILog log)
		{
			this.log = log;
			combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
		}

		public void ReceiveDatagram(TimeMs now, ReadOnlySpan<byte> payload)
		{
			log.Debug("Received datagram of {Size}", payload.Length);

			var reader = new OctetReader(payload);

			var pongTimeLowerBits = MonotonicTimeLowerBitsReader.Read(reader);
			CombinedRangesReader.Read(combinedAuthoritativeStepsQueue, reader, log);

			receiveStats.ReceivedPongTime(now, pongTimeLowerBits);
		}
	}
}
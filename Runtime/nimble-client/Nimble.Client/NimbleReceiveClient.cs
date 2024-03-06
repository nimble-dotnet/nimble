using System;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;

namespace Piot.Nimble.Client
{
	public sealed class NimbleReceiveClient
	{
		public readonly CombinedAuthoritativeStepsQueue combinedAuthoritativeStepsQueue;
		private readonly ILog log;

		public NimbleReceiveClient(TickId tickId, ILog log)
		{
			this.log = log;
			combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
		}

		public void ReceiveDatagram(ReadOnlySpan<byte> payload)
		{
			log.Debug("Received datagram of {Size}", payload.Length);

			var reader = new OctetReader(payload);
			CombinedRangesReader.Read(combinedAuthoritativeStepsQueue, reader, log);
		}
	}
}
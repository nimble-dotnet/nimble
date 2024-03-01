using System;
using Nimble.Authoritative.Steps;
using Piot.Tick;

namespace Piot.Nimble.Client
{
	public class NimbleReceiveClient
	{
		public CombinedAuthoritativeStepsQueue combinedAuthoritativeStepsQueue;

		public NimbleReceiveClient(TickId tickId)
		{
			combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
		}
		
		public void ReceiveDatagram(ReadOnlySpan<byte> payload)
		{
		}
	}
}
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Stats;

namespace Piot.Nimble.Client
{
	public class NimbleClientReceiveStats
	{
		public DeltaTimeMs lastReceivedRoundTripDeltaTime;
		private StatCountThreshold statsRoundTripTime = new(4);

		public FormattedStat RoundTripTime => new(StandardFormatter.Format, statsRoundTripTime.Stat);

		public void ReceivedPongTime(TimeMs now, MonotonicTimeLowerBits.MonotonicTimeLowerBits pongTimeLowerBits)
		{
			var pongTime = LowerBitsToMonotonic.LowerBitsToMonotonicMs(now, pongTimeLowerBits);
			var roundTripTimeMs = now.ms - pongTime.ms;
			lastReceivedRoundTripDeltaTime = new((uint)roundTripTimeMs);
			statsRoundTripTime.Add((int)roundTripTimeMs);
		}
	}
}
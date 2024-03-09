using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Stats;

namespace Piot.Nimble.Client
{
	public class NimbleClientReceiveStats
	{
		public DeltaTimeMs lastReceivedRoundTripDeltaTime;
		private readonly StatCountThreshold statsRoundTripTime = new(20);
		private readonly StatPerSecond datagramCountPerSecond;

		public FormattedStat RoundTripTime => new(MillisecondsFormatter.Format, statsRoundTripTime.Stat);

		public FormattedStat DatagramCountPerSecond =>
			new(StandardFormatterPerSecond.Format, datagramCountPerSecond.Stat);

		public NimbleClientReceiveStats(TimeMs now)
		{
			datagramCountPerSecond = new StatPerSecond(now, new(500));
		}

		public void ReceivedPongTime(TimeMs now, MonotonicTimeLowerBits.MonotonicTimeLowerBits pongTimeLowerBits)
		{
			var pongTime = LowerBitsToMonotonic.LowerBitsToMonotonicMs(now, pongTimeLowerBits);
			var roundTripTimeMs = now.ms - pongTime.ms;
			lastReceivedRoundTripDeltaTime = new((uint)roundTripTimeMs);
			statsRoundTripTime.Add((int)roundTripTimeMs);
			datagramCountPerSecond.Add(1);
			datagramCountPerSecond.Update(now);
		}
	}
}
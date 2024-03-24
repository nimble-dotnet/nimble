using Piot.Discoid;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Stats;

namespace Piot.Nimble.Client
{
	/// <summary>
	/// Holds the statistics for receive related data in the client.
	/// </summary>
	public class NimbleClientReceiveStats
	{
		private readonly StatCountThreshold statsRoundTripTime = new(50);
		private readonly StatPerSecond datagramCountPerSecond;
		public readonly OverwriteCircularBuffer<int> roundTripTimes = new(120);

		/// <summary>
		/// Gets the formatted round trip time statistic.
		/// </summary>
		public FormattedStat RoundTripTime => new(MillisecondsFormatter.Format, statsRoundTripTime.Stat);

		/// <summary>
		/// Gets the formatted datagram count per second statistic.
		/// </summary>
		public FormattedStat DatagramCountPerSecond =>
			new(StandardFormatterPerSecond.Format, datagramCountPerSecond.Stat);


		/// <summary>
		/// Initializes a new instance of the <see cref="NimbleClientReceiveStats"/> class.
		/// </summary>
		/// <param name="now">The current time.</param>
		public NimbleClientReceiveStats(TimeMs now)
		{
			datagramCountPerSecond = new StatPerSecond(now, new(500));
		}

		/// <summary>
		/// Updates statistics upon receiving a pong (a reply from a previous ping).
		/// </summary>
		/// <param name="now">The current time.</param>
		/// <param name="pongTimeLowerBits">The lower bits of the pong time.</param>
		public void ReceivedPongTime(TimeMs now, MonotonicTimeLowerBits.MonotonicTimeLowerBits pongTimeLowerBits)
		{
			var pongTime = LowerBitsToMonotonic.LowerBitsToMonotonicMs(now, pongTimeLowerBits);
			var roundTripTimeMs = now.ms - pongTime.ms;
			statsRoundTripTime.Add((int)roundTripTimeMs);
			datagramCountPerSecond.Add(1);
			datagramCountPerSecond.Update(now);
			ref var roundTripTimeRef = ref roundTripTimes.EnqueueRef();
			roundTripTimeRef = (int)roundTripTimeMs;
		}

		/// <summary>
		/// Updates statistics for dropped datagrams.
		/// </summary>
		/// <param name="datagramCount">The number of dropped datagrams.</param>
		public void DroppedDatagrams(uint datagramCount)
		{
			for (var i = 0; i < datagramCount; ++i)
			{
				roundTripTimes.Enqueue(-1);
			}
		}
	}
}
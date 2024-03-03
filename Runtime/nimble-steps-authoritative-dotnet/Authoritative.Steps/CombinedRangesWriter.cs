using System;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
	public class CombinedRangesWriter
	{
		public static void Write(CombinedAuthoritativeSteps combinedAuthoritativeSteps, TickIdRanges ranges,
			IOctetWriter outStream)
		{
			//TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);
			outStream.WriteUInt8((byte)ranges.ranges.Count);

			foreach (var range in ranges.ranges)
			{
				TickIdRangeWriter.Write(outStream, range);

				var rangeCombinedAuthoritativeSteps = combinedAuthoritativeSteps.FromRange(range);
				if(rangeCombinedAuthoritativeSteps.Count != range.Length)
				{
					throw new Exception($"internal error in CombinedRangesWriter. Passed in range {range}, but received {rangeCombinedAuthoritativeSteps.Count}");
				}

				foreach (var combinedAuthoritativeStep in rangeCombinedAuthoritativeSteps)
				{
					CombinedWriter.Write(combinedAuthoritativeStep, outStream);
				}
			}
		}
	}
}
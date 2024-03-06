using System;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
	public class CombinedRangesWriter
	{
		public static void Write(CombinedAuthoritativeSteps combinedAuthoritativeSteps, TickIdRanges ranges,
			IOctetWriter outStream, ILog log)
		{
			//TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);
			outStream.WriteUInt8((byte)ranges.ranges.Count);

			foreach (var range in ranges.ranges)
			{
				log.Debug("writing combined authoritative range {Range}", range);
				TickIdRangeWriter.Write(outStream, range);

				var rangeCombinedAuthoritativeSteps = combinedAuthoritativeSteps.FromRange(range);
				if(rangeCombinedAuthoritativeSteps.Count != range.Length)
				{
					throw new Exception($"internal error in CombinedRangesWriter. Passed in range {range}, but received {rangeCombinedAuthoritativeSteps.Count}");
				}

				foreach (var combinedAuthoritativeStep in rangeCombinedAuthoritativeSteps)
				{
	//				log.Debug("writing combined authoritative step {{CombinedAuthoritativeStep}}", combinedAuthoritativeStep);
					CombinedWriter.Write(combinedAuthoritativeStep, outStream);
				}
			}
		}
	}
}
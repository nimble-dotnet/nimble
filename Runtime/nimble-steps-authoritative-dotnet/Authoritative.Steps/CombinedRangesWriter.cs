using System;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
	public class CombinedRangesWriter
	{
		public static void Write(CombinedAuthoritativeStepsQueue combinedAuthoritativeSteps, TickIdRanges ranges,
			OctetWriter outStream, ILog log)
		{
			//TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);
			outStream.WriteUInt8((byte)ranges.ranges.Count);

			foreach (var range in ranges.ranges)
			{
				//log.DebugLowLevel("writing combined authoritative range {Range}", range);
				TickIdRangeWriter.Write(outStream, range);

				if (range.Length == 0)
				{
					throw new Exception($"internal error");
				}
				var rangeCombinedAuthoritativeSteps = combinedAuthoritativeSteps.FromRange(range);
				var count = 0u;
				foreach (var combinedAuthoritativeStep in rangeCombinedAuthoritativeSteps)
				{
	//				log.Debug("writing combined authoritative step {{CombinedAuthoritativeStep}}", combinedAuthoritativeStep);
					CombinedWriter.Write(combinedAuthoritativeStep, outStream);
					count++;
				}

				if (count != range.Length)
				{
					throw new Exception($"internal error, enumerator {count} is not same length as range {range.Length}");
				}
			}
		}
	}
}
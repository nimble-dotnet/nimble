using System;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
	public class CombinedRangesReader
	{
		public static void Read(CombinedAuthoritativeStepsQueue combinedAuthoritativeSteps, TickIdRanges ranges,
			IOctetReader reader, ILog log)
		{
			//TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);
			var rangesCount = reader.ReadUInt8();
			for (var i = 0; i < rangesCount; ++i)
			{
				var tickIdRange = TickIdRangeReader.Read(reader);

				for (var stepIndex = 0u; stepIndex < tickIdRange.Length; ++stepIndex)
				{
					var tickId = tickIdRange.startTickId + stepIndex;
					var combinedAuthoritativeStep =
						CombinedReader.DeserializeCombinedAuthoritativeStep(tickId, reader, log);
					if(tickId == combinedAuthoritativeSteps.WaitingForTickId)
					{
						log.Debug("adding authoritative step {{TickId}}", tickId);
						combinedAuthoritativeSteps.Add(combinedAuthoritativeStep);
					}
					else
					{
						log.Debug("ignoring authoritative step {{TickId}}", tickId);
					}
				}
			}
		}
	}
}
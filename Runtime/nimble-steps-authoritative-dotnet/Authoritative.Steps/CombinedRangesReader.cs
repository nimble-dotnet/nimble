using System;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
	public class CombinedRangesReader
	{
		public static void Read(CombinedAuthoritativeStepsQueue combinedAuthoritativeSteps,
			IOctetReader reader, ILog log)
		{
			//TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);
			var rangesCount = reader.ReadUInt8();
			log.Debug("Read combined authoritative steps {{RangeCount}}", rangesCount);

			for (var i = 0; i < rangesCount; ++i)
			{
				var tickIdRange = TickIdRangeReader.Read(reader);
				log.Debug("Read combined authoritative {{Index}} {{Range}}", i, tickIdRange);

				for (var stepIndex = 0u; stepIndex < tickIdRange.Length; ++stepIndex)
				{
					var tickId = tickIdRange.startTickId + stepIndex;
					var combinedAuthoritativeStep =
						CombinedReader.DeserializeCombinedAuthoritativeStep(tickId, reader, log);
						//log.Debug("did read combined authoritative {{combinedAuthoritativeStep}}", combinedAuthoritativeStep);
					if(tickId == combinedAuthoritativeSteps.WaitingForTickId)
					{
						log.Debug("adding authoritative step {{AuthoritativeStep}} to nimble queue", combinedAuthoritativeStep);
						combinedAuthoritativeSteps.Add(combinedAuthoritativeStep);
					}
					else
					{
						log.Notice("ignoring authoritative step {{TickId}}, was waiting for {{WaitingTickId}}", tickId, combinedAuthoritativeSteps.WaitingForTickId);
					}
				}
			}
		}
	}
}
using Piot.Clog;
using Piot.Flood;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
    public static class AuthoritativeStepsReader
    {
        public static uint Read(CombinedAuthoritativeStepsQueue combinedAuthoritativeSteps,
            IOctetReader reader, ILog log)
        {
            //TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);

            var addedCombinedAuthoritativeCount = 0u;
            var startTickId = TickIdReader.Read(reader);

            var count = reader.ReadUInt8();
            //       log.DebugLowLevel("Read combined authoritative {Index} {Range}", i, tickIdRange);

            for (var stepIndex = 0u; stepIndex < count; ++stepIndex)
            {
                var tickId = startTickId + stepIndex;
                var combinedAuthoritativeStep =
                    CombinedReader.DeserializeCombinedAuthoritativeStep(tickId, reader, log);
                //log.Debug("did read combined authoritative {{combinedAuthoritativeStep}}", combinedAuthoritativeStep);
                if (tickId == combinedAuthoritativeSteps.WaitingForTickId)
                {
                    //log.Debug("adding authoritative step {AuthoritativeStep} to nimble queue", combinedAuthoritativeStep);
                    combinedAuthoritativeSteps.Add(combinedAuthoritativeStep);
                    addedCombinedAuthoritativeCount++;
                }
                else
                {
                    //log.Notice("ignoring authoritative step {{TickId}}, was waiting for {{WaitingTickId}}", tickId, combinedAuthoritativeSteps.WaitingForTickId);
                }
            }

            return addedCombinedAuthoritativeCount;
        }
    }
}
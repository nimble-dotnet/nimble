using System;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Piot.Nimble.Authoritative.Steps
{
    public class AuthoritativeStepsWriter
    {
        public static void Write(AuthoritativeStepsQueue authoritativeSteps, TickId fromTickId,
            OctetWriter outStream, ILog log)
        {
            //TickIdWriter.Write(outStream, ranges.ranges[0].startTickId);
            //log.DebugLowLevel("writing combined authoritative range {Range}", range);
            TickIdWriter.Write(outStream, fromTickId);


            var countTell = outStream.Tell;
            outStream.WriteUInt8(0);

            var wholeRange = new TickIdRange(fromTickId, authoritativeSteps.Last.appliedAtTickId);

            var rangeCombinedAuthoritativeSteps = authoritativeSteps.FromRange(wholeRange);
            var count = 0u;
            foreach (var combinedAuthoritativeStep in rangeCombinedAuthoritativeSteps)
            {
                var octetCount = combinedAuthoritativeStep.OctetCount;
                //				log.Debug("writing combined authoritative step {{CombinedAuthoritativeStep}}", combinedAuthoritativeStep);
                if (octetCount + 30 > outStream.OctetsLeft)
                {
                    break;
                }

                AuthoritativeStepWriter.Write(combinedAuthoritativeStep, outStream);
                count++;
            }

            var seekBack = outStream.Tell;
            outStream.Seek(countTell);
            outStream.WriteUInt8((byte)count);
            outStream.Seek(seekBack);
        }
    }
}
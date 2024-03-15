using Piot.Flood;

namespace Nimble.Authoritative.Steps
{
    public class CombinedWriter
    {
        public static void Write(CombinedAuthoritativeStep combinedAuthoritativeStep,
            OctetWriter outStream)
        {
            outStream.WriteUInt8((byte)combinedAuthoritativeStep.authoritativeSteps.Count);

            foreach (var (participantId, authoritativeStep) in combinedAuthoritativeStep.authoritativeSteps)
            {
                var connectState = authoritativeStep.connectState;

                var mask = (byte)0;
                if (connectState != SerializeProviderConnectState.Normal)
                {
                    mask = 0x80;
                }

                outStream.WriteUInt8((byte)(mask | participantId.id));
                if (mask != 0)
                {
                    outStream.WriteUInt8((byte)connectState);
                }
                else
                {
                    outStream.WriteUInt8((byte)authoritativeStep.payload.Length);
                    outStream.WriteOctets(authoritativeStep.payload.Span);
                }
            }
        }
    }
}
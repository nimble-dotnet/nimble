using Piot.Clog;
using Piot.Flood;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	public class CombinedReader
	{
		public static CombinedAuthoritativeStep DeserializeCombinedAuthoritativeStep(TickId tickIdToCompose,
			IOctetReader inStream,
			ILog log)
		{
			var combinedAuthoritativeStep = new CombinedAuthoritativeStep(tickIdToCompose);
			var queueCount = inStream.ReadUInt8();


			for (var i = 0; i < queueCount; ++i)
			{
				var combinedParticipantIdAndMask = inStream.ReadUInt8();
				byte participantId = (byte)0u;
				SerializeProviderConnectState connectState;
				AuthoritativeStep step;

				if((combinedParticipantIdAndMask & 0x80) != 0)
				{
					participantId = (byte)(combinedParticipantIdAndMask & 0x7f);
					connectState = (SerializeProviderConnectState)inStream.ReadUInt8();
					step = new AuthoritativeStep(tickIdToCompose, connectState);
				}
				else
				{
					participantId = combinedParticipantIdAndMask;
					var octetLength = inStream.ReadUInt8();
					var octets = inStream.ReadOctets(octetLength);
					step = new AuthoritativeStep(tickIdToCompose, octets);
				}

				var participant = new ParticipantId { id = participantId };

				combinedAuthoritativeStep.authoritativeSteps.Add(participant, step);
			}

			return combinedAuthoritativeStep;
		}
	}
}
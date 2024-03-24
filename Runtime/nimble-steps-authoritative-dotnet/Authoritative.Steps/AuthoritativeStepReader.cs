using Piot.Clog;
using Piot.Flood;
using Piot.Tick;

namespace Piot.Nimble.Authoritative.Steps
{
	public class AuthoritativeStepReader
	{
		public static AuthoritativeStep ReadOneAuthoritativeStep(TickId tickIdToCompose,
			IOctetReader inStream,
			ILog log)
		{
			var combinedAuthoritativeStep = new AuthoritativeStep(tickIdToCompose);
			var queueCount = inStream.ReadUInt8();

			for (var i = 0; i < queueCount; ++i)
			{
				var combinedParticipantIdAndMask = inStream.ReadUInt8();
				byte participantId = (byte)0u;
				SerializeProviderConnectState connectState;
				AuthoritativeStepForOneParticipant stepForOneParticipant;

				if((combinedParticipantIdAndMask & 0x80) != 0)
				{
					participantId = (byte)(combinedParticipantIdAndMask & 0x7f);
					connectState = (SerializeProviderConnectState)inStream.ReadUInt8();
					stepForOneParticipant = new AuthoritativeStepForOneParticipant(tickIdToCompose, connectState);
				}
				else
				{
					participantId = combinedParticipantIdAndMask;
					var octetLength = inStream.ReadUInt8();
					var octets = inStream.ReadOctets(octetLength);

					stepForOneParticipant = new AuthoritativeStepForOneParticipant(tickIdToCompose, octets);
				}

				var participant = new ParticipantId { id = participantId };

				combinedAuthoritativeStep.authoritativeSteps.Add(participant, stepForOneParticipant);
			}

			return combinedAuthoritativeStep;
		}
	}
}
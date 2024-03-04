using System;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Nimble.Steps;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	public class ParticipantConnections
	{
		public readonly Dictionary<byte, ParticipantConnection> participantConnections = new();

		public ParticipantConnection CreateParticipantConnection(byte connectionId, LocalPlayerIndex localPlayerIndex)
		{
			var newParticipantId = GetFreeParticipantId();
			var newConnection = new ParticipantConnection(newParticipantId, localPlayerIndex);

			participantConnections.Add(newParticipantId.id, newConnection);

			return newConnection;
		}

		public void RemoveParticipantConnection(ParticipantConnection participantConnection)
		{
			participantConnections.Remove(participantConnection.participantId.id);
		}

		public ParticipantConnection GetParticipantConnection(ParticipantId participantId)
		{
			return participantConnections[participantId.id];
		}


		public ParticipantId GetFreeParticipantId()
		{
			for (var i = (byte)0; i < 64; ++i)
			{
				if(!participantConnections.ContainsKey(i))
				{
					return new ParticipantId(i);
				}
			}

			throw new Exception("out of participant Ids");
		}

		public uint PercentageThatHasPredictedStepForAtLeast(TickId tickId)
		{
			if(participantConnections.Count == 0)
			{
				return 0;
			}
			
			var count = 0u;

			foreach (var participant in participantConnections.Values)
			{
				if(participant.incomingSteps.HasStepForAtLeastTickId(tickId))
				{
					count++;
				}
			}

			var percentage = count * 100 / participantConnections.Count;

			return (uint)percentage;
		}
	}
}
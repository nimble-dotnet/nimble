using System;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;

namespace Nimble.Authoritative.Steps
{
	public class ParticipantConnections
	{
		public readonly Dictionary<byte, ParticipantConnection> participantConnections = new();

		public ParticipantConnection CreateParticipantConnection(byte connectionId)
		{
			var newParticipantId = GetFreeParticipantId();
			var newConnection = new ParticipantConnection(newParticipantId);

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
	}
}
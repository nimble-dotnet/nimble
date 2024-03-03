using System.Collections.Generic;
using Piot.Nimble.Steps;

namespace Nimble.Authoritative.Steps
{
	public class ConnectionToParticipants
	{
		public Dictionary<LocalPlayerIndex, ParticipantConnection> connections = new();

		public bool TryGetParticipantConnectionFromLocalPlayer(LocalPlayerIndex localPlayerIndex,
			out ParticipantConnection participantConnection)
		{
			return connections.TryGetValue(localPlayerIndex, out participantConnection);
		}

		public void Add(LocalPlayerIndex localPlayerIndex, ParticipantConnection participantConnection)
		{
			connections.Add(localPlayerIndex, participantConnection);
		}
	}
}
using System.Collections.Generic;
using Piot.Nimble.Steps;

namespace Nimble.Authoritative.Steps
{
	public class ConnectionToParticipants
	{
		public Dictionary<LocalPlayerIndex, Participant> connections = new();

		public bool TryGetParticipantConnectionFromLocalPlayer(LocalPlayerIndex localPlayerIndex,
			out Participant participant)
		{
			return connections.TryGetValue(localPlayerIndex, out participant);
		}

		public void Add(LocalPlayerIndex localPlayerIndex, Participant participant)
		{
			connections.Add(localPlayerIndex, participant);
		}
	}
}
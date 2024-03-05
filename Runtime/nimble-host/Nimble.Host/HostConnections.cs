using System.Collections.Generic;

namespace Nimble.Authoritative.Steps
{
	public class HostConnections
	{
		public Dictionary<byte, HostConnection> connections = new();

		public HostConnection GetOrCreateConnection(byte connectionId)
		{
			var found = connections.TryGetValue(connectionId, out var hostConnection);
			if(found)
			{
				return hostConnection;
			}

			var newConnection = new HostConnection(connectionId);
			connections.Add(connectionId, newConnection);

			return newConnection;
		}
	}
}
/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Generic;

namespace Nimble.Authoritative.Steps
{
    public class HostConnections
    {
        public Dictionary<byte, HostConnection> connections = new();

        public bool TryGetConnection(byte transportConnectionId, out HostConnection hostConnection)
        {
            return connections.TryGetValue(transportConnectionId, out hostConnection);
        }

        public void AddConnection(HostConnection hostConnection)
        {
            connections.Add(hostConnection.transportConnectionId, hostConnection);
        }
    }
}
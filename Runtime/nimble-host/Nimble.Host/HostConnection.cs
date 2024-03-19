/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Piot.MonotonicTimeLowerBits;
using Piot.OrderedDatagrams;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
    public class HostConnection
    {
        public ConnectionToParticipants connectionToParticipants = new();

        public byte connectionId;
        public MonotonicTimeLowerBits lastReceivedMonotonicLowerBits;
        public TickId expectingAuthoritativeTickId;
        public byte dropppedAuthoritativeAfterExpecting;
        public TickId lastReceivedPredictedTickId;
        public OrderedDatagramsSequenceId datagramSequenceIdOut;


        public HostConnection(byte connectionId)
        {
            this.connectionId = connectionId;
        }

        public override string ToString()
        {
            return $"[HostConnection {connectionId} participantCount:{connectionToParticipants.connections.Count}]";
        }
    }
}
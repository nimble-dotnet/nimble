using System;
using System.Collections.Generic;
using Piot.Tick;

#nullable enable

namespace Nimble.Authoritative.Steps
{
  
    public readonly struct AuthoritativeStepForOneParticipant
    {
        public readonly TickId appliedAtTickId;
        public readonly SerializeProviderConnectState connectState;
        public readonly byte[] payload;

        public AuthoritativeStepForOneParticipant(TickId appliedAtTickId, ReadOnlySpan<byte> payload)
        {
            this.appliedAtTickId = appliedAtTickId;
            this.payload = payload.ToArray();
            connectState = SerializeProviderConnectState.Normal;
        }

        public AuthoritativeStepForOneParticipant(TickId appliedAtTickId, SerializeProviderConnectState connectState)
        {
            this.appliedAtTickId = appliedAtTickId;
            payload = default;
            this.connectState = connectState;
        }

        /*
        public override bool Equals(object? obj)
        {
            if(obj is null)
            {
                return false;
            }

            var other = (PredictedStep)obj;

            return other.appliedAtTickId.tickId == appliedAtTickId.tickId &&
                   CompareOctets.Compare(other.payload.Span, payload.Span);
        }
        */

        public override string ToString()
        {
            return
                $"[AuthoritativeStep TickId:{appliedAtTickId} octetSize:{payload.Length} state:{connectState}]";
        }
    }
}
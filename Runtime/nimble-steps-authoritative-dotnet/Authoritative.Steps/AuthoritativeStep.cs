using System;
using System.Collections.Generic;
using Piot.Nimble.Steps;
using Piot.Tick;

#nullable enable

namespace Nimble.Authoritative.Steps
{
	public class CombinedAuthoritativeStep
	{
		public readonly TickId appliedAtTickId;
		public readonly Dictionary<ParticipantId, AuthoritativeStep> authoritativeSteps = new();

		public CombinedAuthoritativeStep(TickId tickId)
		{
			appliedAtTickId = tickId;
		}

		public AuthoritativeStep GetAuthoritativeStep(ParticipantId participantId)
		{
			return authoritativeSteps[participantId];
		}
	}
	
	

	public readonly struct AuthoritativeStep
	{
		public readonly TickId appliedAtTickId;
		public readonly SerializeProviderConnectState connectState;
		public readonly ReadOnlyMemory<byte> payload;

		public AuthoritativeStep(TickId appliedAtTickId, ReadOnlySpan<byte> payload)
		{
			this.appliedAtTickId = appliedAtTickId;
			this.payload = payload.ToArray();
			connectState = SerializeProviderConnectState.Normal;
		}

		public AuthoritativeStep(TickId appliedAtTickId, SerializeProviderConnectState connectState)
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
				$"[PredictedStep TickId:{appliedAtTickId} octetSize:{payload.Length}]";
		}
	}
}
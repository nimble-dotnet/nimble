using System;
using System.Collections.Generic;
using System.Linq;
using Piot.Nimble.Steps;
using Piot.Tick;
using UnityEngine;

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

		public bool TryGetAuthoritativeStep(ParticipantId participantId, out AuthoritativeStep authoritativeStep)
		{
			return authoritativeSteps.TryGetValue(participantId, out authoritativeStep);
		}

		public AuthoritativeStep GetAuthoritativeStep(ParticipantId participantId)
		{
			return authoritativeSteps[participantId];
		}

		public static string AuthoritativeStepsToString(Dictionary<ParticipantId, AuthoritativeStep> steps)
		{
			if(steps.Count == 0)
			{
				return "  completely empty";
			}
			
			var s = "";
			
			foreach (var (participantId, authoritativeStep) in steps)
			{
				s += $"\n {participantId}: {authoritativeStep}";
			}

			return s;
		}

		public override string ToString()
		{
			return $"{appliedAtTickId}: {AuthoritativeStepsToString(authoritativeSteps)}";
		}
	}

	/*
	public class CombinedAuthoritativeSteps
	{
		private IEnumerable<CombinedAuthoritativeStep> steps;
		private TickIdRange range;

		public CombinedAuthoritativeSteps(IEnumerable<CombinedAuthoritativeStep> steps, TickIdRange range)
		{
			this.steps = steps;
			this.range = range;
		}

		public IReadOnlyCollection<CombinedAuthoritativeStep> FromRange(TickIdRange filterRange)
		{
			if(!filterRange.Contains(filterRange))
			{
				throw new Exception($"can not query {filterRange} from {range}");
			}

			var startIndex = range.Offset(filterRange);

			return steps.Skip((int)startIndex).Take((int)filterRange.Length).ToArray();
		}
	}
*/

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
				$"[AuthoritativeStep TickId:{appliedAtTickId} octetSize:{payload.Length} state:{connectState}]";
		}
	}
}
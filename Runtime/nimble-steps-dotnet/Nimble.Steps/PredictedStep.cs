/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Piot.Tick;


namespace Piot.Nimble.Steps
{
	public static class CompareOctets
	{
		public static bool Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
		{
			return a.SequenceEqual(b);
		}
	}


	public class PredictedStepsLocalPlayers
	{
		public Dictionary<byte, PredictedStepsQueue> predictedStepsQueues = new();

		public PredictedStepsQueue GetStepsQueueForLocalPlayer(LocalPlayerIndex playerIndex)
		{
			return predictedStepsQueues[playerIndex.Value];
		}

		public void CreateLocalPlayer(LocalPlayerIndex playerIndex)
		{
			predictedStepsQueues.Add(playerIndex.Value, new PredictedStepsQueue());
		}
	}

	public readonly struct PredictedStepsForAllLocalPlayers
	{
		public readonly PredictedStepsForPlayer[] stepsForEachPlayerInSequence;

		public TickId debugFirstId => stepsForEachPlayerInSequence.Length == 0 ||
		                              stepsForEachPlayerInSequence[0].steps.Length == 0
			? default
			: stepsForEachPlayerInSequence[0].steps[0].appliedAtTickId;

		public TickId debugLastId => stepsForEachPlayerInSequence.Length == 0 ||
		                             stepsForEachPlayerInSequence[0].steps.Length == 0
			? default
			: stepsForEachPlayerInSequence[0].steps[^1].appliedAtTickId;

		public PredictedStepsForAllLocalPlayers(PredictedStepsForPlayer[] stepsForEachPlayerInSequence)
		{
			this.stepsForEachPlayerInSequence = stepsForEachPlayerInSequence;
		}

		private static string PredictedStepsForPlayers(PredictedStepsForPlayer[] stepsForEachPlayerInSequence)
		{
			var s = "";

			foreach (var stepsForPlayer in stepsForEachPlayerInSequence)
			{
				s += $"\n  {stepsForPlayer}";
			}

			return s;
		}

		public override string ToString()
		{
			return $"[PredictedFromLocalPlayers {PredictedStepsForPlayers(stepsForEachPlayerInSequence)}";
		}
	}

	public struct PredictedStepsForPlayer
	{
		public PredictedStep[] steps;
		public LocalPlayerIndex localPlayerIndex;

		public PredictedStepsForPlayer(LocalPlayerIndex localPlayerIndex,
			PredictedStep[] steps)
		{
			this.localPlayerIndex = localPlayerIndex;
			this.steps = steps;
		}

		private static string PredictedStepsToString(PredictedStep[] predictedSteps)
		{
			var s = "";

			foreach (var predictedStep in predictedSteps)
			{
				s += $"\n  {predictedStep}";
			}

			return s;
		}

		public override string ToString()
		{
			return $"[PredictedStepsForPlayer {localPlayerIndex}: {PredictedStepsToString(steps)}]";
		}
	}


	/// <summary>
	///     Serialized Game specific input step in the <see cref="PredictedStep.payload" />.
	/// </summary>
	public readonly struct PredictedStep
	{
		public readonly TickId appliedAtTickId;
		public readonly ReadOnlyMemory<byte> payload;

		public PredictedStep(TickId appliedAtTickId, ReadOnlySpan<byte> payload)
		{
			this.appliedAtTickId = appliedAtTickId;
			this.payload = payload.ToArray();
		}

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

		public override string ToString()
		{
			byte debugByte = 0;
			
			if (payload.Length > 9)
			{
				debugByte = payload.Span[9];
			}
			return
				$"[PredictedStep TickId:{appliedAtTickId} octetSize:{payload.Length} debug:{debugByte}]";
		}
	}
}
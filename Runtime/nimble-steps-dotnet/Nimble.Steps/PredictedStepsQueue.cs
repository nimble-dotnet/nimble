/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Piot.Discoid;
using Piot.Tick;

namespace Piot.Nimble.Steps
{
	/// <summary>
	///     Queue of serialized game specific inputs.
	/// </summary>
	public sealed class PredictedStepsQueue
	{
		readonly CircularBuffer<PredictedStep> queue = new(48);

		TickId waitingForTickId;

		public IEnumerable<PredictedStep> Collection => queue;

		public int Count => queue.Count;

		public bool IsEmpty => queue.IsEmpty;

		public bool IsFull => queue.IsFull;

		public TickId WaitingForTickId => waitingForTickId;

		public bool IsInitialized { get; private set; }

		public PredictedStep Last => queue.Last;
		public TickIdRange Range => new TickIdRange(queue.Peek().appliedAtTickId, queue.Last.appliedAtTickId);

		public PredictedStep Peek()
		{
			return queue.Peek();
		}

		public void AddPredictedStep(PredictedStep predictedStep)
		{
			if(!IsInitialized)
			{
				waitingForTickId = predictedStep.appliedAtTickId;
				IsInitialized = true;
			}
			else
			{
				if(predictedStep.appliedAtTickId.tickId < waitingForTickId.tickId)
				{
					throw new Exception(
						$"you tried to add a prediction for a step that has already passed in the prediction queue. was waiting for {waitingForTickId} and you provided {predictedStep.appliedAtTickId}");
				}

				if(predictedStep.appliedAtTickId.tickId > waitingForTickId.tickId)
				{
					// TickId can only go up, so it means that we have dropped inputs at previous ticks
					// We can only remove all inputs and add this one
					Reset();
				}
			}

			queue.Enqueue(predictedStep);
			waitingForTickId = new(predictedStep.appliedAtTickId.tickId + 1);
		}

		public void Reset()
		{
			queue.Clear();
		}

		public bool HasStepForTickId(TickId tickId)
		{
			if(queue.Count == 0)
			{
				return false;
			}

			var firstTickId = queue.Peek().appliedAtTickId.tickId;
			var lastTick = waitingForTickId.tickId - 1;

			return tickId.tickId >= firstTickId && tickId.tickId <= lastTick;
		}

		public PredictedStep GetInputFromTickId(TickId tickId)
		{
			foreach (var input in queue)
			{
				if(input.appliedAtTickId == tickId)
				{
					return input;
				}
			}

			throw new ArgumentOutOfRangeException(nameof(tickId), "tick id is not found in queue");
		}

		public void DiscardUpToAndExcluding(TickId tickId)
		{
			while (queue.Count > 0)
			{
				if(queue.Peek().appliedAtTickId.tickId < tickId.tickId)
				{
					queue.Dequeue();
				}
				else
				{
					break;
				}
			}
		}

		public override string ToString()
		{
			return queue.Count == 0
				? "[PredictedStepsQueue empty]"
				: $"[PredictedStepsQueue first: {Peek().appliedAtTickId} last: {waitingForTickId.tickId - 1}]";
		}
	}
}
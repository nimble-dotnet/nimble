/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	/// <summary>
	/// </summary>
	public sealed class CombinedAuthoritativeStepsQueue
	{
		readonly Queue<CombinedAuthoritativeStep> queue = new();

		TickId waitingForTickId;

		public CombinedAuthoritativeStep[] Collection => queue.ToArray();

		public int Count => queue.Count;

		public TickId WaitingForTickId => waitingForTickId;

		public CombinedAuthoritativeStep Last => queue.Last();

		public CombinedAuthoritativeStep Peek()
		{
			return queue.Peek();
		}

		public CombinedAuthoritativeStepsQueue(TickId tickId)
		{
			waitingForTickId = tickId;
		}

		public void Add(CombinedAuthoritativeStep authoritativeStep)
		{
			if(authoritativeStep.appliedAtTickId != waitingForTickId)
			{
				throw new Exception(
					$"authoritative steps can not have gaps. waiting for {waitingForTickId}, but received {authoritativeStep.appliedAtTickId}");
			}

			queue.Enqueue(authoritativeStep);
			waitingForTickId = new(authoritativeStep.appliedAtTickId.tickId + 1);
		}

		public void Reset()
		{
			queue.Clear();
		}

		/*
		public bool HasInputForTickId(TickId tickId)
		{
		    if (queue.Count == 0)
		    {
		        return false;
		    }

		    var firstTickId = queue.Peek().appliedAtTickId.tickId;
		    var lastTick = waitingForTickId.tickId - 1;

		    return tickId.tickId >= firstTickId && tickId.tickId <= lastTick;
		}

		public CombinedAuthoritativeStep GetInputFromTickId(TickId tickId)
		{
		    foreach (var input in queue)
		    {
		        if (input.appliedAtTickId == tickId)
		        {
		            return input;
		        }
		    }

		    throw new ArgumentOutOfRangeException(nameof(tickId), "tick id is not found in queue");
		}
		*/

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

		public CombinedAuthoritativeStep Dequeue()
		{
			return queue.Dequeue();
		}
	}
}
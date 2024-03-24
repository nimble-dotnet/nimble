/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Piot.Discoid;
using Piot.Tick;

namespace Piot.Nimble.Authoritative.Steps
{
	/// <summary>
	/// </summary>
	public sealed class AuthoritativeStepsQueue
	{
		readonly OverwriteCircularBuffer<AuthoritativeStep> queue = new(60);

		TickId waitingForTickId;

		public IEnumerable<AuthoritativeStep> Collection => queue;

		public int Count => queue.Count;

		public int Capacity => queue.Capacity;

		public bool IsEmpty => Count == 0;

		public TickId WaitingForTickId => waitingForTickId;

		public AuthoritativeStep Last => queue.Last;

		public TickIdRange Range => new(queue.Peek().appliedAtTickId, queue.Last.appliedAtTickId);

		public AuthoritativeStepsQueue(TickId tickId)
		{
			waitingForTickId = tickId;
		}

		public void Add(AuthoritativeStep authoritativeStep)
		{
			if(authoritativeStep.appliedAtTickId != waitingForTickId)
			{
				throw new Exception(
					$"authoritative steps can not have gaps. waiting for {waitingForTickId}, but received {authoritativeStep.appliedAtTickId}");
			}

			queue.Enqueue(authoritativeStep);
			waitingForTickId = new(authoritativeStep.appliedAtTickId.tickId + 1);
		}

		public AuthoritativeStep Dequeue()
		{
			return queue.Dequeue();
		}

		public IEnumerable<AuthoritativeStep> FromRange(TickIdRange range)
		{
			if(queue.IsEmpty)
			{
				return default;
			}

			var startTickId = queue.Peek().appliedAtTickId;
			var lastTickId = queue.Last.appliedAtTickId;

			var availableRange = new TickIdRange(startTickId, lastTickId);

			var rangeToSend = availableRange.Satisfy(range);

			var count = rangeToSend.Length;
			var offset = rangeToSend.startTickId.tickId - startTickId.tickId;
			var enumerator = queue.GetRangeEnumerator(offset, count);
			return new EnumeratorWrapper<AuthoritativeStep>(enumerator);
		}
	}
}